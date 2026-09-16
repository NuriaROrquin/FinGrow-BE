using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoPagoOAuthGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_synced_at",
                table: "employee_integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oauth_access_token",
                table: "employee_integrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "oauth_expires_at",
                table: "employee_integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oauth_refresh_token",
                table: "employee_integrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_synced_at",
                table: "employee_integrations");

            migrationBuilder.DropColumn(
                name: "oauth_access_token",
                table: "employee_integrations");

            migrationBuilder.DropColumn(
                name: "oauth_expires_at",
                table: "employee_integrations");

            migrationBuilder.DropColumn(
                name: "oauth_refresh_token",
                table: "employee_integrations");
        }
    }
}
