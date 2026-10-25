using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE repetidos text;
                BEGIN
                    SELECT string_agg(employees.email, ', ' ORDER BY employees.email)
                    INTO repetidos
                    FROM employees
                    JOIN companies ON companies.email = employees.email;

                    IF repetidos IS NOT NULL THEN
                        RAISE EXCEPTION 'Hay emails repetidos entre empleados y empresas: %. Hay que corregirlos antes de crear la tabla users.', repetidos;
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    two_factor_secret = table.Column<string>(type: "text", nullable: true),
                    two_factor_enabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO users (id, email, password_hash, role, is_active, last_login_at, two_factor_secret, two_factor_enabled_at, created_at, updated_at)
                SELECT id, email, password_hash, 'Employee', is_active, last_login_at, two_factor_secret, two_factor_enabled_at, created_at, updated_at
                FROM employees;

                INSERT INTO users (id, email, password_hash, role, is_active, last_login_at, two_factor_secret, two_factor_enabled_at, created_at, updated_at)
                SELECT id, email, password_hash, 'Company', is_active, NULL, two_factor_secret, two_factor_enabled_at, created_at, updated_at
                FROM companies;

                DELETE FROM refresh_tokens
                WHERE user_id NOT IN (SELECT id FROM users);
                """);

            migrationBuilder.DropIndex(
                name: "ix_employees_email",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_companies_email",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "email",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled_at",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "two_factor_secret",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "email",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled_at",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "two_factor_secret",
                table: "companies");

            migrationBuilder.AddForeignKey(
                name: "fk_companies_users_id",
                table: "companies",
                column: "id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_users_id",
                table: "employees",
                column: "id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_users_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_companies_users_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "fk_employees_users_id",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_users_user_id",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "employees",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "employees",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at",
                table: "employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "two_factor_enabled_at",
                table: "employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "two_factor_secret",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "companies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "two_factor_enabled_at",
                table: "companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "two_factor_secret",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE employees
                SET email = users.email,
                    password_hash = users.password_hash,
                    is_active = users.is_active,
                    last_login_at = users.last_login_at,
                    two_factor_secret = users.two_factor_secret,
                    two_factor_enabled_at = users.two_factor_enabled_at
                FROM users
                WHERE users.id = employees.id;

                UPDATE companies
                SET email = users.email,
                    password_hash = users.password_hash,
                    is_active = users.is_active,
                    two_factor_secret = users.two_factor_secret,
                    two_factor_enabled_at = users.two_factor_enabled_at
                FROM users
                WHERE users.id = companies.id;
                """);

            foreach (var table in new[] { "employees", "companies" })
            {
                migrationBuilder.AlterColumn<string>(
                    name: "email",
                    table: table,
                    type: "character varying(256)",
                    maxLength: 256,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "character varying(256)",
                    oldMaxLength: 256,
                    oldNullable: true);

                migrationBuilder.AlterColumn<string>(
                    name: "password_hash",
                    table: table,
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "character varying(200)",
                    oldMaxLength: 200,
                    oldNullable: true);

                migrationBuilder.AlterColumn<bool>(
                    name: "is_active",
                    table: table,
                    type: "boolean",
                    nullable: false,
                    oldClrType: typeof(bool),
                    oldType: "boolean",
                    oldNullable: true);
            }

            migrationBuilder.CreateIndex(
                name: "ix_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_email",
                table: "companies",
                column: "email",
                unique: true);

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
