using CarshiTow.Domain.Entities;
using CarshiTow.Domain.Enums;
using CarshiTow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarshiTow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PhoneNumber).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.PhoneNumber).IsUnique();
        builder.OwnsOne(x => x.Password, p =>
        {
            p.Property(v => v.Value).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        });
        builder.Property(x => x.Role).HasColumnName("role").HasColumnType("user_role").IsRequired();
        builder.Property(x => x.IsMfaEnabled).HasColumnName("mfa_enabled").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.MfaSecret).HasColumnName("mfa_secret").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("user_status").IsRequired();
        builder.Property(x => x.LastLoginAtUtc).HasColumnName("last_login_at");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");
        builder.Property(x => x.TowYardId).HasColumnName("tow_yard_id");
        builder.HasIndex(x => x.TowYardId);
        builder.HasOne(x => x.TowYard)
            .WithMany(x => x.StaffUsers)
            .HasForeignKey(x => x.TowYardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at");
        builder.Property(x => x.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(128);
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UsedAtUtc).HasColumnName("used_at");
        builder.HasOne(x => x.User).WithMany(x => x.PasswordResetTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Fingerprint)
            .HasConversion(fp => fp.Value, v => new DeviceFingerprint(v))
            .HasColumnName("fingerprint")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(128).IsRequired();
        builder.Property(x => x.IsTrusted).HasColumnName("is_trusted").IsRequired();
        builder.Property(x => x.LastSeenAtUtc).HasColumnName("last_seen_at");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.HasIndex(x => new { x.UserId, x.Fingerprint }).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.Devices).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.CodeHash).HasColumnName("code_hash").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasConversion<int>();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at");
        builder.HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAtUtc });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TowYardConfiguration : IEntityTypeConfiguration<TowYard>
{
    public void Configure(EntityTypeBuilder<TowYard> builder)
    {
        builder.ToTable("tow_yards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(x => x.BusinessName).HasColumnName("business_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Abn).HasColumnName("abn").HasMaxLength(11).IsRequired();
        builder.HasIndex(x => x.Abn).IsUnique();
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Suburb).HasColumnName("suburb").HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasColumnName("state").HasColumnType("au_state").IsRequired();
        builder.Property(x => x.Postcode).HasColumnName("postcode").HasMaxLength(4).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("tow_yard_status").IsRequired();
        builder.Property(x => x.VerificationDocsUrl).HasColumnName("verification_docs_url");
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at");
        builder.Property(x => x.VerifiedByUserId).HasColumnName("verified_by_user_id");
        builder.Property(x => x.BankBsb).HasColumnName("bank_bsb").HasMaxLength(6);
        builder.Property(x => x.BankAccountNumber).HasColumnName("bank_account_number").HasMaxLength(20);
        builder.Property(x => x.StripeConnectId).HasColumnName("stripe_connect_id").HasMaxLength(255);
        builder.Property(x => x.PlatformFeeOverride).HasColumnName("platform_fee_override");
        builder.Property(x => x.LogoUrl).HasColumnName("logo_url");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");
        builder.HasOne(x => x.OwnerUser).WithMany(x => x.OwnedTowYards).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.VerifiedByUser).WithMany(x => x.VerifiedTowYards).HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PhotoPackConfiguration : IEntityTypeConfiguration<PhotoPack>
{
    public void Configure(EntityTypeBuilder<PhotoPack> builder)
    {
        builder.ToTable("photo_packs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TowYardId).HasColumnName("tow_yard_id").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.VehicleRego).HasColumnName("vehicle_rego").HasMaxLength(10).IsRequired();
        builder.Property(x => x.VehicleRegoState).HasColumnName("vehicle_rego_state").HasColumnType("au_state").IsRequired();
        builder.Property(x => x.VehicleMake).HasColumnName("vehicle_make").HasMaxLength(100).IsRequired();
        builder.Property(x => x.VehicleModel).HasColumnName("vehicle_model").HasMaxLength(100).IsRequired();
        builder.Property(x => x.VehicleYear).HasColumnName("vehicle_year").IsRequired();
        builder.Property(x => x.VehicleVin).HasColumnName("vehicle_vin").HasMaxLength(17);
        builder.Property(x => x.ClaimReference).HasColumnName("claim_reference").HasMaxLength(100);
        builder.Property(x => x.TowYardReference).HasColumnName("tow_yard_reference").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("photo_pack_status").IsRequired();
        builder.Property(x => x.PhotoCount).HasColumnName("photo_count").HasDefaultValue((short)0);
        builder.Property(x => x.QualityScore).HasColumnName("quality_score");
        builder.Property(x => x.DamageSeverity).HasColumnName("damage_severity").HasColumnType("damage_severity");
        builder.Property(x => x.TotalLossProbability).HasColumnName("total_loss_probability").HasPrecision(5, 2);
        builder.Property(x => x.AiDamageDescription).HasColumnName("ai_damage_description");
        builder.Property(x => x.TowYardPriceCents).HasColumnName("tow_yard_price_cents").IsRequired();
        builder.Property(x => x.PlatformFeeCents).HasColumnName("platform_fee_cents").HasDefaultValue(5500).IsRequired();
        builder.Property(x => x.TotalPriceCents).HasColumnName("total_price_cents").IsRequired();
        builder.Property(x => x.LinkToken).HasColumnName("link_token").HasMaxLength(64);
        builder.HasIndex(x => x.LinkToken).IsUnique();
        builder.Property(x => x.LinkExpiresAtUtc).HasColumnName("link_expires_at");
        builder.Property(x => x.LinkViewCount).HasColumnName("link_view_count").HasDefaultValue(0);
        builder.Property(x => x.PaidAtUtc).HasColumnName("paid_at");
        builder.Property(x => x.PaidByEmail).HasColumnName("paid_by_email").HasMaxLength(255);
        builder.Property(x => x.StripePaymentIntentId).HasColumnName("stripe_payment_intent_id").HasMaxLength(255);
        builder.Property(x => x.FraudFlagged).HasColumnName("fraud_flagged").HasDefaultValue(false);
        builder.Property(x => x.FraudFlaggedReason).HasColumnName("fraud_flagged_reason");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");
        builder.HasOne(x => x.TowYard).WithMany(x => x.PhotoPacks).HasForeignKey(x => x.TowYardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedPhotoPacks).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PackId).HasColumnName("pack_id").IsRequired();
        builder.Property(x => x.OriginalS3Key).HasColumnName("original_s3_key").IsRequired();
        builder.Property(x => x.PreviewS3Key).HasColumnName("preview_s3_key");
        builder.Property(x => x.ThumbnailS3Key).HasColumnName("thumbnail_s3_key");
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.WidthPx).HasColumnName("width_px").IsRequired();
        builder.Property(x => x.HeightPx).HasColumnName("height_px").IsRequired();
        builder.Property(x => x.AiCategory).HasColumnName("ai_category").HasColumnType("photo_category");
        builder.Property(x => x.ManualCategoryOverride).HasColumnName("manual_category_override").HasDefaultValue(false);
        builder.Property(x => x.QualityScore).HasColumnName("quality_score");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue((short)0);
        builder.Property(x => x.TakenAtUtc).HasColumnName("taken_at");
        builder.Property(x => x.GpsLat).HasColumnName("gps_lat").HasPrecision(9, 6);
        builder.Property(x => x.GpsLng).HasColumnName("gps_lng").HasPrecision(9, 6);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");
        builder.HasOne(x => x.Pack).WithMany(x => x.Photos).HasForeignKey(x => x.PackId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PackId).HasColumnName("pack_id").IsRequired();
        builder.Property(x => x.TowYardId).HasColumnName("tow_yard_id").IsRequired();
        builder.Property(x => x.StripePaymentIntentId).HasColumnName("stripe_payment_intent_id").HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.StripePaymentIntentId).IsUnique();
        builder.Property(x => x.StripeChargeId).HasColumnName("stripe_charge_id").HasMaxLength(255);
        builder.Property(x => x.TotalAmountCents).HasColumnName("total_amount_cents").IsRequired();
        builder.Property(x => x.PlatformFeeCents).HasColumnName("platform_fee_cents").IsRequired();
        builder.Property(x => x.TowYardAmountCents).HasColumnName("tow_yard_amount_cents").IsRequired();
        builder.Property(x => x.StripeFeeCents).HasColumnName("stripe_fee_cents").IsRequired();
        builder.Property(x => x.NetToTowYardCents).HasColumnName("net_to_tow_yard_cents").IsRequired();
        builder.Property(x => x.InsurerEmail).HasColumnName("insurer_email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.InsurerOrgName).HasColumnName("insurer_org_name").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("transaction_status").IsRequired();
        builder.Property(x => x.RefundAmountCents).HasColumnName("refund_amount_cents");
        builder.Property(x => x.RefundReason).HasColumnName("refund_reason");
        builder.Property(x => x.RefundedByUserId).HasColumnName("refunded_by_user_id");
        builder.Property(x => x.InvoiceS3Key).HasColumnName("invoice_s3_key");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");
        builder.HasOne(x => x.Pack).WithMany(x => x.Transactions).HasForeignKey(x => x.PackId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TowYard).WithMany(x => x.Transactions).HasForeignKey(x => x.TowYardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RefundedByUser).WithMany(x => x.RefundedTransactions).HasForeignKey(x => x.RefundedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("payouts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TowYardId).HasColumnName("tow_yard_id").IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(x => x.TransactionCount).HasColumnName("transaction_count").IsRequired();
        builder.Property(x => x.GrossAmountCents).HasColumnName("gross_amount_cents").IsRequired();
        builder.Property(x => x.ProcessingFeeCents).HasColumnName("processing_fee_cents").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.NetAmountCents).HasColumnName("net_amount_cents").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("payout_status").IsRequired();
        builder.Property(x => x.BankReference).HasColumnName("bank_reference").HasMaxLength(255);
        builder.Property(x => x.InitiatedAtUtc).HasColumnName("initiated_at");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");
        builder.HasOne(x => x.TowYard).WithMany(x => x.Payouts).HasForeignKey(x => x.TowYardId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent");
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at").IsRequired();
        builder.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
