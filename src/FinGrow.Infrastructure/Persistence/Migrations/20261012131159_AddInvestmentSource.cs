using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_reference",
                table: "investments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "investments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateIndex(
                name: "ix_investments_employee_id_source_external_reference",
                table: "investments",
                columns: new[] { "employee_id", "source", "external_reference" },
                unique: true,
                filter: "external_reference IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_investments_external_reference_when_imported",
                table: "investments",
                sql: "(source = 'Manual') = (external_reference IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_investments_employee_id_source_external_reference",
                table: "investments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_investments_external_reference_when_imported",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "external_reference",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "source",
                table: "investments");
        }
    }
}
