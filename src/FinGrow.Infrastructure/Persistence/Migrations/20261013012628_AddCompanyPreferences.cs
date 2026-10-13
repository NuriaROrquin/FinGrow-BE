using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "fiscal_year_start_month",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "companies",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "es");

            migrationBuilder.AddColumn<string>(
                name: "theme",
                table: "companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Light");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fiscal_year_start_month",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "language",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "theme",
                table: "companies");
        }
    }
}
