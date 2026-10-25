using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Hu78BudgetCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "budgets",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "ARS");

            migrationBuilder.Sql("""
                UPDATE budgets AS budget
                SET currency = limits.currency
                FROM (
                    SELECT DISTINCT ON (budget_id) budget_id, currency
                    FROM budget_category_limits
                    ORDER BY budget_id, id
                ) AS limits
                WHERE budget.id = limits.budget_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency",
                table: "budgets");
        }
    }
}
