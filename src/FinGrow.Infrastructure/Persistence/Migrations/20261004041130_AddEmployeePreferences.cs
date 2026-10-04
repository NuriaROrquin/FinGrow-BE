using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: los empleados que ya existian quedan con los mismos valores con los que
            // nace un empleado nuevo (Employee.Create).
            migrationBuilder.AddColumn<string>(
                name: "date_format",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DayMonthYear");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "es");

            migrationBuilder.AddColumn<string>(
                name: "theme",
                table: "employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "System");

            // Los defaults eran solo para rellenar las filas viejas. Se sacan para que un INSERT
            // que se olvide de una preferencia falle en vez de inventarla.
            migrationBuilder.Sql(
                "ALTER TABLE employees ALTER COLUMN date_format DROP DEFAULT, " +
                "ALTER COLUMN language DROP DEFAULT, ALTER COLUMN theme DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "date_format",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "language",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "theme",
                table: "employees");
        }
    }
}
