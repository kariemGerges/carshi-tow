using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarshiTow.Infrastructure.Seeding;

public static class CrashifyAdminSeed
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var settings = services.GetRequiredService<IOptions<CrashifySeedSettings>>().Value;
        if (!settings.Enabled ||
            string.IsNullOrWhiteSpace(settings.Email) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("CrashifyAdminSeed");
        var repo = services.GetRequiredService<IUserRepository>();
        var email = settings.Email.Trim().ToLowerInvariant();
        if (await repo.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return;
        }

        var hasher = services.GetRequiredService<IPasswordHasher>();
        var phone = settings.PhoneNumber.Trim();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PhoneNumber = phone,
            Password = new HashedPassword(hasher.Hash(settings.Password)),
            Role = UserRole.CrashifyAdmin,
            Status = UserStatus.Active,
            IsMfaEnabled = true,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await repo.AddAsync(user, cancellationToken);
        await repo.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded Crashify admin user with email {Email}.", email);
    }
}
