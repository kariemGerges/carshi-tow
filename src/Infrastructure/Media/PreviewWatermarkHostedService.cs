using Amazon.S3;
using Amazon.S3.Model;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLaborsImageSharpColor = SixLabors.ImageSharp.Color;

namespace CarshiTow.Infrastructure.Media;

public sealed class PreviewWatermarkHostedService(
    PreviewJobChannel holder,
    IServiceScopeFactory scopeFactory,
    ILogger<PreviewWatermarkHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var photoId in holder.Work.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessOneAsync(photoId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Preview generation failed for photo {PhotoId}", photoId);
            }
        }
    }

    private async Task ProcessOneAsync(Guid photoId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s3 = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
        var signer = scope.ServiceProvider.GetRequiredService<IOriginalsUploadSigner>();
        var storage = scope.ServiceProvider.GetRequiredService<IOptions<ObjectStorageSettings>>().Value;

        var photo = await db.Photos
            .Include(p => p.Pack)
            .FirstOrDefaultAsync(p => p.Id == photoId && p.DeletedAtUtc == null, cancellationToken);
        if (photo is null)
        {
            return;
        }

        using var get = await s3.GetObjectAsync(storage.BucketName, photo.OriginalS3Key, cancellationToken);
        await using var inputStream = get.ResponseStream;
        using var image = await Image.LoadAsync<Rgba32>(inputStream, cancellationToken);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(1920, 1080),
            Mode = ResizeMode.Max,
        }));

        var expiry = photo.Pack.LinkExpiresAtUtc?.ToString("yyyy-MM-dd") ?? "—";
        var watermark =
            $"CRASHI-TOW PREVIEW\nPack {photo.PackId:D}\nExpires {expiry} UTC\nViewer ref {Guid.NewGuid():N}".TrimEnd();

        var families = SystemFonts.Collection.Families.ToArray();
        var familyName = families.Length > 0 ? families[0].Name : "Arial";
        var font = SystemFonts.CreateFont(familyName, 22, FontStyle.Bold);
        var size = TextMeasurer.MeasureSize(watermark, new TextOptions(font));
        image.Mutate(ctx => ctx.DrawText(
            watermark,
            font,
            SixLaborsImageSharpColor.White,
            new PointF(12, image.Height - size.Height - 12)));

        await using var jpegOut = new MemoryStream();
        await image.SaveAsJpegAsync(jpegOut, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 60 }, cancellationToken);
        jpegOut.Position = 0;

        var previewKey = signer.BuildPreviewKey(photo.PackId, photo.Id);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = storage.BucketName,
            Key = previewKey,
            InputStream = jpegOut,
            ContentType = "image/jpeg",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        }, cancellationToken);

        photo.PreviewS3Key = previewKey;
        photo.ThumbnailS3Key = previewKey;
        photo.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
