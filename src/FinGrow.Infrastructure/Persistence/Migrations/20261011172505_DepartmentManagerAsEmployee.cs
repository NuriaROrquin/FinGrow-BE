using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentManagerAsEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "manager_id",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_manager_id",
                table: "departments",
                column: "manager_id");

            migrationBuilder.AddForeignKey(
                name: "fk_departments_employees_manager_id",
                table: "departments",
                column: "manager_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                UPDATE departments d
                SET manager_id = matches.employee_id
                FROM (
                    SELECT d2.id AS department_id, MIN(e.id::text)::uuid AS employee_id
                    FROM departments d2
                    JOIN employees e
                        ON e.company_id = d2.company_id
                        AND lower(e.full_name) = lower(btrim(d2.manager_name))
                    WHERE d2.manager_name IS NOT NULL
                    GROUP BY d2.id
                    HAVING COUNT(*) = 1
                ) AS matches
                WHERE d.id = matches.department_id;
                """);

            migrationBuilder.DropColumn(
                name: "manager_name",
                table: "departments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "manager_name",
                table: "departments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE departments d
                SET manager_name = left(e.full_name, 150)
                FROM employees e
                WHERE e.id = d.manager_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_departments_employees_manager_id",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "ix_departments_manager_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "manager_id",
                table: "departments");
        }
    }
}
