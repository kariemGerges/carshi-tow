using CarshiTow.Domain.Entities;
using CarshiTow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarshiTow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.OwnsOne(x => x.Password, p => p.Property(v => v.Value).HasColumnName("PasswordHash").HasMaxLength(200).IsRequired());
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
    }
}

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Fingerprint)
            .HasConversion(fp => fp.Value, v => new DeviceFingerprint(v))
            .HasColumnName("Fingerprint")
            .HasMaxLength(128)
            .IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Fingerprint }).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.Devices).HasForeignKey(x => x.UserId);
    }
}

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodeHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Purpose).HasConversion<int>();
        builder.HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAtUtc });
    }
}
