using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_account_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_integrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_integrations_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "integration_link_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_link_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_link_codes_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_integrations_employee_id_provider",
                table: "employee_integrations",
                columns: new[] { "employee_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_integrations_provider_external_account_id",
                table: "employee_integrations",
                columns: new[] { "provider", "external_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_link_codes_employee_id",
                table: "integration_link_codes",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_link_codes_provider_code_hash",
                table: "integration_link_codes",
                columns: new[] { "provider", "code_hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_integrations");

            migrationBuilder.DropTable(
                name: "integration_link_codes");
        }
    }
}
