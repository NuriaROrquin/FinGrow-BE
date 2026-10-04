using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledJobsAndMetricsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_metrics_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    active_employees = table.Column<int>(type: "integer", nullable: false),
                    participating_employees = table.Column<int>(type: "integer", nullable: false),
                    confirmed_transactions = table.Column<int>(type: "integer", nullable: false),
                    employees_with_budget = table.Column<int>(type: "integer", nullable: false),
                    employees_with_active_goal = table.Column<int>(type: "integer", nullable: false),
                    goals_achieved = table.Column<int>(type: "integer", nullable: false),
                    employees_with_integration = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_metrics_snapshots", x => x.id);
                    table.CheckConstraint("ck_company_metrics_snapshots_period_starts_on_day_one", "extract(day from period_start) = 1");
                    table.ForeignKey(
                        name: "fk_company_metrics_snapshots_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "department_metrics_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    active_employees = table.Column<int>(type: "integer", nullable: false),
                    participating_employees = table.Column<int>(type: "integer", nullable: false),
                    confirmed_transactions = table.Column<int>(type: "integer", nullable: false),
                    employees_with_budget = table.Column<int>(type: "integer", nullable: false),
                    employees_with_active_goal = table.Column<int>(type: "integer", nullable: false),
                    goals_achieved = table.Column<int>(type: "integer", nullable: false),
                    employees_with_integration = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_department_metrics_snapshots", x => x.id);
                    table.CheckConstraint("ck_department_metrics_snapshots_period_starts_on_day_one", "extract(day from period_start) = 1");
                    table.ForeignKey(
                        name: "fk_department_metrics_snapshots_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_department_metrics_snapshots_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_runs", x => x.id);
                    table.CheckConstraint("ck_job_runs_finished_after_started", "finished_at IS NULL OR finished_at >= started_at");
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_metrics_snapshots_company_id_period_start",
                table: "company_metrics_snapshots",
                columns: new[] { "company_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_department_metrics_snapshots_company_id_period_start",
                table: "department_metrics_snapshots",
                columns: new[] { "company_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_department_metrics_snapshots_department_id_period_start",
                table: "department_metrics_snapshots",
                columns: new[] { "department_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_runs_job_name_started_at",
                table: "job_runs",
                columns: new[] { "job_name", "started_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_metrics_snapshots");

            migrationBuilder.DropTable(
                name: "department_metrics_snapshots");

            migrationBuilder.DropTable(
                name: "job_runs");
        }
    }
}
