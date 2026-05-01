using System.Net;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace CarshiTow.Infrastructure.Repositories;

public sealed class AuditLogWriter(AppDbContext db, IHttpContextAccessor http) : IAuditLogWriter
{
    public async Task WriteAsync(
        Guid? actingUserId,
        string action,
        string entityType,
        Guid? entityId,
        IPAddress? ipAddress,
        string? userAgent,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        var hc = http.HttpContext;
        var ip = ipAddress ?? hc?.Connection.RemoteIpAddress;
        var ua = userAgent ?? hc?.Request.Headers.UserAgent.ToString();

        await db.AuditLogs.AddAsync(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actingUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ip,
                UserAgent = string.IsNullOrWhiteSpace(ua) ? null : ua,
                Metadata = metadataJson,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);
    }
}
