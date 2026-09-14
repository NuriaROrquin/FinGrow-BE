using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeriveTotalsFromParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Se borran columnas sin mover sus datos: la base todavia esta vacia.
            migrationBuilder.DropCheckConstraint(
                name: "ck_investments_same_currency",
                table: "investments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goals_same_currency",
                table: "goals");

            migrationBuilder.DropIndex(
                name: "ix_budgets_employee_id_category_period_start",
                table: "budgets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_budgets_limit_positive",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "current_value",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "current_value_currency",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "valued_at",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "current_amount",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "current_currency",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "category",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "limit_amount",
                table: "budgets");

            migrationBuilder.CreateTable(
                name: "budget_category_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    limit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_category_limits", x => x.id);
                    table.CheckConstraint("ck_budget_category_limits_limit_positive", "limit_amount > 0");
                    table.ForeignKey(
                        name: "fk_budget_category_limits_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goal_contributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    contributed_on = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goal_contributions", x => x.id);
                    table.CheckConstraint("ck_goal_contributions_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_goal_contributions_goals_goal_id",
                        column: x => x.goal_id,
                        principalTable: "goals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "investment_valuations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valued_on = table.Column<DateOnly>(type: "date", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investment_valuations", x => x.id);
                    table.ForeignKey(
                        name: "fk_investment_valuations_investments_investment_id",
                        column: x => x.investment_id,
                        principalTable: "investments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_budgets_employee_id_period_period_start",
                table: "budgets",
                columns: new[] { "employee_id", "period", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_category_limits_budget_id_category",
                table: "budget_category_limits",
                columns: new[] { "budget_id", "category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_goal_contributions_goal_id_contributed_on",
                table: "goal_contributions",
                columns: new[] { "goal_id", "contributed_on" });

            migrationBuilder.CreateIndex(
                name: "ix_investment_valuations_investment_id_valued_on",
                table: "investment_valuations",
                columns: new[] { "investment_id", "valued_on" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_category_limits");

            migrationBuilder.DropTable(
                name: "goal_contributions");

            migrationBuilder.DropTable(
                name: "investment_valuations");

            migrationBuilder.DropIndex(
                name: "ix_budgets_employee_id_period_period_start",
                table: "budgets");

            migrationBuilder.AddColumn<decimal>(
                name: "current_value",
                table: "investments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "current_value_currency",
                table: "investments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valued_at",
                table: "investments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "current_amount",
                table: "goals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "current_currency",
                table: "goals",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "budgets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "budgets",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "limit_amount",
                table: "budgets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_investments_same_currency",
                table: "investments",
                sql: "invested_currency = current_value_currency");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goals_same_currency",
                table: "goals",
                sql: "target_currency = current_currency");

            migrationBuilder.CreateIndex(
                name: "ix_budgets_employee_id_category_period_start",
                table: "budgets",
                columns: new[] { "employee_id", "category", "period_start" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_budgets_limit_positive",
                table: "budgets",
                sql: "limit_amount > 0");
        }
    }
}
