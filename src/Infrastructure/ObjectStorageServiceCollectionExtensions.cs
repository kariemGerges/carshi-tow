using Amazon.S3;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using CarshiTow.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CarshiTow.Infrastructure;

public static class ObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddCarshiTowObjectStorage(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(sp =>
            AmazonS3ClientFactory.Create(sp.GetRequiredService<IOptions<ObjectStorageSettings>>().Value));
        services.AddSingleton<IOriginalsUploadSigner, S3OriginalsUploadSigner>();
        return services;
    }
}
