using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentSymbolAndQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "investments",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "symbol",
                table: "investments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_investments_quantity_positive",
                table: "investments",
                sql: "quantity IS NULL OR quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_investments_symbol_with_quantity",
                table: "investments",
                sql: "(symbol IS NULL) = (quantity IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_investments_quantity_positive",
                table: "investments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_investments_symbol_with_quantity",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "symbol",
                table: "investments");
        }
    }
}
