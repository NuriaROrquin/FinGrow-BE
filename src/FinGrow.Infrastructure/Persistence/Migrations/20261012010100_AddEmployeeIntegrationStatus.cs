using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIntegrationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_reason",
                table: "employee_integrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "employee_integrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_integrations_error_has_reason",
                table: "employee_integrations",
                sql: "(status = 'Error') = (error_reason IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_integrations_error_has_reason",
                table: "employee_integrations");

            migrationBuilder.DropColumn(
                name: "error_reason",
                table: "employee_integrations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "employee_integrations");
        }
    }
}
