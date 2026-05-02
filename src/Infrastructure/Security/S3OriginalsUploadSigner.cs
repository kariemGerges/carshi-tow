using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace CarshiTow.Infrastructure.Security;

public sealed class S3OriginalsUploadSigner(IOptions<ObjectStorageSettings> options, IAmazonS3 client) : IOriginalsUploadSigner
{
    public SignedPutObjectResult CreateSignedPut(string objectKey, string contentType, TimeSpan ttl)
    {
        var o = options.Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = o.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = contentType,
            Protocol = o.ServiceUrl is Uri u && string.Equals(u.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                ? Protocol.HTTP
                : Protocol.HTTPS
        };

        var url = client.GetPreSignedURL(request);
        return new SignedPutObjectResult(url);
    }

    public string CreateSignedGet(string objectKey, TimeSpan ttl)
    {
        var o = options.Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = o.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = o.ServiceUrl is Uri u && string.Equals(u.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                ? Protocol.HTTP
                : Protocol.HTTPS
        };

        return client.GetPreSignedURL(request);
    }

    public string BuildOriginalKey(Guid packId, Guid photoId, string safeFileExtensionIncludingDotLower)
    {
        var prefix = options.Value.OriginalsPrefix.Trim().Trim('/').ToLowerInvariant();
        return $"{prefix}/{packId:D}/{photoId:D}{safeFileExtensionIncludingDotLower}";
    }

    public string BuildPreviewKey(Guid packId, Guid photoId)
    {
        var prefix = options.Value.PreviewsPrefix.Trim().Trim('/').ToLowerInvariant();
        return $"{prefix}/{packId:D}/{photoId:D}.jpg";
    }
}

public static class AmazonS3ClientFactory
{
    public static IAmazonS3 Create(ObjectStorageSettings o)
    {
        if (o.ServiceUrl is Uri baseUri)
        {
            var cfg = new AmazonS3Config
            {
                ServiceURL = baseUri.ToString(),
                ForcePathStyle = o.ForcePathStyle,
                AuthenticationRegion = o.Region,
                UseHttp = string.Equals(baseUri.Scheme, "http", StringComparison.OrdinalIgnoreCase)
            };

            var ak = FirstNonEmpty(o.AwsAccessKey, Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"), "test");
            var sk = FirstNonEmpty(o.AwsSecretAccessKey, Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"), "test");
            var creds = new BasicAWSCredentials(ak, sk);
            return new AmazonS3Client(creds, cfg);
        }

        var region = RegionEndpoint.GetBySystemName(string.IsNullOrWhiteSpace(o.Region) ? "ap-southeast-2" : o.Region);
        return new AmazonS3Client(region);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return "test";
    }
}
