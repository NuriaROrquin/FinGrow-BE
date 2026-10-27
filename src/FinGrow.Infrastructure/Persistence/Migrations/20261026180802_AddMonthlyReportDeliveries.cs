using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyReportDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "enabled_at",
                table: "company_notification_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "monthly_report_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    outbound_email_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monthly_report_deliveries", x => x.id);
                    table.CheckConstraint("ck_monthly_report_deliveries_period_starts_on_day_one", "extract(day from period_start) = 1");
                    table.ForeignKey(
                        name: "fk_monthly_report_deliveries_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_monthly_report_deliveries_outbound_emails_outbound_email_id",
                        column: x => x.outbound_email_id,
                        principalTable: "outbound_emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                "UPDATE company_notification_settings SET enabled_at = updated_at WHERE is_enabled;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_company_notification_settings_enabled_at_matches_is_enabled",
                table: "company_notification_settings",
                sql: "(is_enabled AND enabled_at IS NOT NULL) OR (NOT is_enabled AND enabled_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_report_deliveries_company_id_period_start",
                table: "monthly_report_deliveries",
                columns: new[] { "company_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monthly_report_deliveries_outbound_email_id",
                table: "monthly_report_deliveries",
                column: "outbound_email_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monthly_report_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_company_notification_settings_enabled_at_matches_is_enabled",
                table: "company_notification_settings");

            migrationBuilder.DropColumn(
                name: "enabled_at",
                table: "company_notification_settings");

            migrationBuilder.Sql(
                "DELETE FROM company_notification_settings WHERE type = 'MonthlyReport';");
        }
    }
}
