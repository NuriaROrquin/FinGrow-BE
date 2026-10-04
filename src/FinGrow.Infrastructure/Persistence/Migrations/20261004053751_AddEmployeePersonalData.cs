using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePersonalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                table: "employees",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "national_id",
                table: "employees");
        }
    }
}
