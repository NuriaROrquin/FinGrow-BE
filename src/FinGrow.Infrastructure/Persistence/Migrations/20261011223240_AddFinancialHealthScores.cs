using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialHealthScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "employees_with_score",
                table: "department_metrics_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "financial_health_score_sum",
                table: "department_metrics_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "employees_with_score",
                table: "company_metrics_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "financial_health_score_sum",
                table: "company_metrics_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "financial_health_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    savings_factor = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    budget_factor = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    goals_factor = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cushion_factor = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_health_scores", x => x.id);
                    table.CheckConstraint("ck_financial_health_scores_factors_range", "savings_factor between 0 and 100 and cushion_factor between 0 and 100 and (budget_factor is null or budget_factor between 0 and 100) and (goals_factor is null or goals_factor between 0 and 100)");
                    table.CheckConstraint("ck_financial_health_scores_period_starts_on_day_one", "extract(day from period_start) = 1");
                    table.CheckConstraint("ck_financial_health_scores_score_range", "score between 0 and 100");
                    table.ForeignKey(
                        name: "fk_financial_health_scores_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_financial_health_scores_employee_id_period_start",
                table: "financial_health_scores",
                columns: new[] { "employee_id", "period_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_health_scores");

            migrationBuilder.DropColumn(
                name: "employees_with_score",
                table: "department_metrics_snapshots");

            migrationBuilder.DropColumn(
                name: "financial_health_score_sum",
                table: "department_metrics_snapshots");

            migrationBuilder.DropColumn(
                name: "employees_with_score",
                table: "company_metrics_snapshots");

            migrationBuilder.DropColumn(
                name: "financial_health_score_sum",
                table: "company_metrics_snapshots");
        }
    }
}
