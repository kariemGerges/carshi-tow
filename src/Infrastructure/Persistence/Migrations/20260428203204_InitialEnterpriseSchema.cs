using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEnterpriseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:au_state.australian_state", "nsw,vic,qld,wa,sa,tas,nt,act")
                .Annotation("Npgsql:Enum:damage_severity.damage_severity", "low,moderate,severe,probable_total_loss")
                .Annotation("Npgsql:Enum:payout_status.payout_status", "pending,processing,completed,failed")
                .Annotation("Npgsql:Enum:photo_category.photo_category", "front,rear,left,right,interior,engine,undercarriage,odometer,other")
                .Annotation("Npgsql:Enum:photo_pack_status.photo_pack_status", "draft,pending_processing,active,paid,expired,flagged")
                .Annotation("Npgsql:Enum:tow_yard_status.tow_yard_status", "pending,active,suspended")
                .Annotation("Npgsql:Enum:transaction_status.transaction_status", "pending,succeeded,failed,refunded,partially_refunded")
                .Annotation("Npgsql:Enum:user_role.user_role", "tow_yard_admin,tow_yard_staff,insurer,crashify_admin,crashify_support")
                .Annotation("Npgsql:Enum:user_status.user_status", "active,suspended,pending_verification");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    mfa_secret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role = table.Column<int>(type: "user_role", nullable: false),
                    status = table.Column<int>(type: "user_status", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "otp_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_otp_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tow_yards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    abn = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    suburb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<int>(type: "au_state", nullable: false),
                    postcode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<int>(type: "tow_yard_status", nullable: false),
                    verification_docs_url = table.Column<string[]>(type: "text[]", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bank_bsb = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    stripe_connect_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    platform_fee_override = table.Column<int>(type: "integer", nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tow_yards", x => x.id);
                    table.ForeignKey(
                        name: "FK_tow_yards_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tow_yards_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tow_yard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    transaction_count = table.Column<short>(type: "smallint", nullable: false),
                    gross_amount_cents = table.Column<int>(type: "integer", nullable: false),
                    processing_fee_cents = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    net_amount_cents = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "payout_status", nullable: false),
                    bank_reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    initiated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payouts", x => x.id);
                    table.ForeignKey(
                        name: "FK_payouts_tow_yards_tow_yard_id",
                        column: x => x.tow_yard_id,
                        principalTable: "tow_yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photo_packs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tow_yard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_rego = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    vehicle_rego_state = table.Column<int>(type: "au_state", nullable: false),
                    vehicle_make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vehicle_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vehicle_year = table.Column<short>(type: "smallint", nullable: false),
                    vehicle_vin = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    claim_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tow_yard_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "photo_pack_status", nullable: false),
                    photo_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    quality_score = table.Column<short>(type: "smallint", nullable: true),
                    damage_severity = table.Column<int>(type: "damage_severity", nullable: true),
                    total_loss_probability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ai_damage_description = table.Column<string>(type: "text", nullable: true),
                    tow_yard_price_cents = table.Column<int>(type: "integer", nullable: false),
                    platform_fee_cents = table.Column<int>(type: "integer", nullable: false, defaultValue: 5500),
                    total_price_cents = table.Column<int>(type: "integer", nullable: false),
                    link_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    link_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    link_view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_by_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fraud_flagged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fraud_flagged_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_packs", x => x.id);
                    table.ForeignKey(
                        name: "FK_photo_packs_tow_yards_tow_yard_id",
                        column: x => x.tow_yard_id,
                        principalTable: "tow_yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_photo_packs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_s3_key = table.Column<string>(type: "text", nullable: false),
                    preview_s3_key = table.Column<string>(type: "text", nullable: true),
                    thumbnail_s3_key = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size_bytes = table.Column<int>(type: "integer", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    width_px = table.Column<short>(type: "smallint", nullable: false),
                    height_px = table.Column<short>(type: "smallint", nullable: false),
                    ai_category = table.Column<int>(type: "photo_category", nullable: true),
                    manual_category_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    quality_score = table.Column<short>(type: "smallint", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gps_lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    gps_lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_photos_photo_packs_pack_id",
                        column: x => x.pack_id,
                        principalTable: "photo_packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tow_yard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stripe_charge_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    total_amount_cents = table.Column<int>(type: "integer", nullable: false),
                    platform_fee_cents = table.Column<int>(type: "integer", nullable: false),
                    tow_yard_amount_cents = table.Column<int>(type: "integer", nullable: false),
                    stripe_fee_cents = table.Column<int>(type: "integer", nullable: false),
                    net_to_tow_yard_cents = table.Column<int>(type: "integer", nullable: false),
                    insurer_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    insurer_org_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "transaction_status", nullable: false),
                    refund_amount_cents = table.Column<int>(type: "integer", nullable: true),
                    refund_reason = table.Column<string>(type: "text", nullable: true),
                    refunded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_s3_key = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_photo_packs_pack_id",
                        column: x => x.pack_id,
                        principalTable: "photo_packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_tow_yards_tow_yard_id",
                        column: x => x.tow_yard_id,
                        principalTable: "tow_yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_users_refunded_by_user_id",
                        column: x => x.refunded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_devices_user_id_fingerprint",
                table: "devices",
                columns: new[] { "user_id", "fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_otp_codes_user_id_purpose_created_at",
                table: "otp_codes",
                columns: new[] { "user_id", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payouts_tow_yard_id",
                table: "payouts",
                column: "tow_yard_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_packs_created_by_user_id",
                table: "photo_packs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_packs_link_token",
                table: "photo_packs",
                column: "link_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photo_packs_tow_yard_id",
                table: "photo_packs",
                column: "tow_yard_id");

            migrationBuilder.CreateIndex(
                name: "IX_photos_pack_id",
                table: "photos",
                column: "pack_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tow_yards_abn",
                table: "tow_yards",
                column: "abn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tow_yards_owner_user_id",
                table: "tow_yards",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tow_yards_verified_by_user_id",
                table: "tow_yards",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_pack_id",
                table: "transactions",
                column: "pack_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_refunded_by_user_id",
                table: "transactions",
                column: "refunded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_stripe_payment_intent_id",
                table: "transactions",
                column: "stripe_payment_intent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_tow_yard_id",
                table: "transactions",
                column: "tow_yard_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "otp_codes");

            migrationBuilder.DropTable(
                name: "payouts");

            migrationBuilder.DropTable(
                name: "photos");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "photo_packs");

            migrationBuilder.DropTable(
                name: "tow_yards");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
