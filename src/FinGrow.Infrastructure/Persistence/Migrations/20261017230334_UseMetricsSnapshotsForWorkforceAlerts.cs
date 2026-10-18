using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseMetricsSnapshotsForWorkforceAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE company_notification_settings
                SET cadence = 'Monthly'
                WHERE type IN ('EmployeeAdded', 'EmployeeRemoved')
                  AND cadence <> 'Monthly';
                """);

            migrationBuilder.DropTable(
                name: "employee_lifecycle_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_lifecycle_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_lifecycle_events_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_events_company_id_type_occurred_at",
                table: "employee_lifecycle_events",
                columns: new[] { "company_id", "type", "occurred_at" });
        }
    }
}
