using CarshiTow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarshiTow.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<TowYard> TowYards => Set<TowYard>();
    public DbSet<PhotoPack> PhotoPacks => Set<PhotoPack>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Domain.Enums.UserRole>("user_role");
        modelBuilder.HasPostgresEnum<Domain.Enums.UserStatus>("user_status");
        modelBuilder.HasPostgresEnum<Domain.Enums.AustralianState>("au_state");
        modelBuilder.HasPostgresEnum<Domain.Enums.TowYardStatus>("tow_yard_status");
        modelBuilder.HasPostgresEnum<Domain.Enums.PhotoPackStatus>("photo_pack_status");
        modelBuilder.HasPostgresEnum<Domain.Enums.DamageSeverity>("damage_severity");
        modelBuilder.HasPostgresEnum<Domain.Enums.PhotoCategory>("photo_category");
        modelBuilder.HasPostgresEnum<Domain.Enums.TransactionStatus>("transaction_status");
        modelBuilder.HasPostgresEnum<Domain.Enums.PayoutStatus>("payout_status");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
