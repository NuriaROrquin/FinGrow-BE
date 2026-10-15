using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentAnnualRateAndTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "annual_rate",
                table: "investments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "term_days",
                table: "investments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_investments_annual_rate_positive",
                table: "investments",
                sql: "annual_rate IS NULL OR annual_rate > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_investments_term_with_rate",
                table: "investments",
                sql: "term_days IS NULL OR (annual_rate IS NOT NULL AND term_days > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_investments_annual_rate_positive",
                table: "investments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_investments_term_with_rate",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "annual_rate",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "term_days",
                table: "investments");
        }
    }
}
