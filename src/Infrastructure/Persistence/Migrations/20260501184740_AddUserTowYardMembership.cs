using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTowYardMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tow_yard_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_tow_yard_id",
                table: "users",
                column: "tow_yard_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_tow_yards_tow_yard_id",
                table: "users",
                column: "tow_yard_id",
                principalTable: "tow_yards",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_tow_yards_tow_yard_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_tow_yard_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tow_yard_id",
                table: "users");
        }
    }
}
