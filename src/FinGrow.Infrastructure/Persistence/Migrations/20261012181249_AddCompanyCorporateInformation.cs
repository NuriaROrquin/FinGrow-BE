using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCorporateInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fiscal_address",
                table: "companies",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "founded_year",
                table: "companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "industry",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fiscal_address",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "founded_year",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "industry",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "companies");
        }
    }
}
