using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDepartmentAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_department_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_department_assignments", x => x.id);
                    table.CheckConstraint("ck_employee_department_assignments_period", "ended_at IS NULL OR ended_at >= started_at");
                    table.ForeignKey(
                        name: "fk_employee_department_assignments_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_department_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_department_assignments_department_id_started_at",
                table: "employee_department_assignments",
                columns: new[] { "department_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_department_assignments_employee_id_current",
                table: "employee_department_assignments",
                column: "employee_id",
                unique: true,
                filter: "ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employee_department_assignments_employee_id_started_at",
                table: "employee_department_assignments",
                columns: new[] { "employee_id", "started_at" });

            migrationBuilder.Sql(
                """
                INSERT INTO employee_department_assignments (id, employee_id, department_id, started_at)
                SELECT gen_random_uuid(), id, department_id, created_at
                FROM employees
                WHERE department_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_department_assignments");
        }
    }
}
