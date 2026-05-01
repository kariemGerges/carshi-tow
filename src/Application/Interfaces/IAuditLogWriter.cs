using System.Net;

namespace CarshiTow.Application.Interfaces;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid? actingUserId,
        string action,
        string entityType,
        Guid? entityId,
        IPAddress? ipAddress,
        string? userAgent,
        string? metadataJson,
        CancellationToken cancellationToken);
}
