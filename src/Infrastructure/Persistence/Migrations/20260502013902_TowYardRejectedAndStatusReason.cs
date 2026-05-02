using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TowYardRejectedAndStatusReason : Migration
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
                .Annotation("Npgsql:Enum:tow_yard_status.tow_yard_status", "pending,active,suspended,rejected")
                .Annotation("Npgsql:Enum:transaction_status.transaction_status", "pending,succeeded,failed,refunded,partially_refunded")
                .Annotation("Npgsql:Enum:user_role.user_role", "tow_yard_admin,tow_yard_staff,insurer,crashify_admin,crashify_support")
                .Annotation("Npgsql:Enum:user_status.user_status", "active,suspended,pending_verification")
                .OldAnnotation("Npgsql:Enum:au_state.australian_state", "nsw,vic,qld,wa,sa,tas,nt,act")
                .OldAnnotation("Npgsql:Enum:damage_severity.damage_severity", "low,moderate,severe,probable_total_loss")
                .OldAnnotation("Npgsql:Enum:payout_status.payout_status", "pending,processing,completed,failed")
                .OldAnnotation("Npgsql:Enum:photo_category.photo_category", "front,rear,left,right,interior,engine,undercarriage,odometer,other")
                .OldAnnotation("Npgsql:Enum:photo_pack_status.photo_pack_status", "draft,pending_processing,active,paid,expired,flagged")
                .OldAnnotation("Npgsql:Enum:tow_yard_status.tow_yard_status", "pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:transaction_status.transaction_status", "pending,succeeded,failed,refunded,partially_refunded")
                .OldAnnotation("Npgsql:Enum:user_role.user_role", "tow_yard_admin,tow_yard_staff,insurer,crashify_admin,crashify_support")
                .OldAnnotation("Npgsql:Enum:user_status.user_status", "active,suspended,pending_verification");

            migrationBuilder.AddColumn<string>(
                name: "last_status_change_reason",
                table: "tow_yards",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_status_change_reason",
                table: "tow_yards");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:au_state.australian_state", "nsw,vic,qld,wa,sa,tas,nt,act")
                .Annotation("Npgsql:Enum:damage_severity.damage_severity", "low,moderate,severe,probable_total_loss")
                .Annotation("Npgsql:Enum:payout_status.payout_status", "pending,processing,completed,failed")
                .Annotation("Npgsql:Enum:photo_category.photo_category", "front,rear,left,right,interior,engine,undercarriage,odometer,other")
                .Annotation("Npgsql:Enum:photo_pack_status.photo_pack_status", "draft,pending_processing,active,paid,expired,flagged")
                .Annotation("Npgsql:Enum:tow_yard_status.tow_yard_status", "pending,active,suspended")
                .Annotation("Npgsql:Enum:transaction_status.transaction_status", "pending,succeeded,failed,refunded,partially_refunded")
                .Annotation("Npgsql:Enum:user_role.user_role", "tow_yard_admin,tow_yard_staff,insurer,crashify_admin,crashify_support")
                .Annotation("Npgsql:Enum:user_status.user_status", "active,suspended,pending_verification")
                .OldAnnotation("Npgsql:Enum:au_state.australian_state", "nsw,vic,qld,wa,sa,tas,nt,act")
                .OldAnnotation("Npgsql:Enum:damage_severity.damage_severity", "low,moderate,severe,probable_total_loss")
                .OldAnnotation("Npgsql:Enum:payout_status.payout_status", "pending,processing,completed,failed")
                .OldAnnotation("Npgsql:Enum:photo_category.photo_category", "front,rear,left,right,interior,engine,undercarriage,odometer,other")
                .OldAnnotation("Npgsql:Enum:photo_pack_status.photo_pack_status", "draft,pending_processing,active,paid,expired,flagged")
                .OldAnnotation("Npgsql:Enum:tow_yard_status.tow_yard_status", "pending,active,suspended,rejected")
                .OldAnnotation("Npgsql:Enum:transaction_status.transaction_status", "pending,succeeded,failed,refunded,partially_refunded")
                .OldAnnotation("Npgsql:Enum:user_role.user_role", "tow_yard_admin,tow_yard_staff,insurer,crashify_admin,crashify_support")
                .OldAnnotation("Npgsql:Enum:user_status.user_status", "active,suspended,pending_verification");
        }
    }
}
