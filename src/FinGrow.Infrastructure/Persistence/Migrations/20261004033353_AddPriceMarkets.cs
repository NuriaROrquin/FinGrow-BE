using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceMarkets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_security_prices_symbol_currency",
                table: "security_prices");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "security_prices",
                type: "numeric(30,12)",
                precision: 30,
                scale: 12,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,6)",
                oldPrecision: 20,
                oldScale: 6);

            migrationBuilder.AlterColumn<string>(
                name: "symbol",
                table: "security_prices",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "market",
                table: "security_prices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Exchange");

            migrationBuilder.AlterColumn<string>(
                name: "symbol",
                table: "investments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "investments",
                type: "numeric(30,10)",
                precision: 30,
                scale: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,6)",
                oldPrecision: 20,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_prices_market_symbol_currency",
                table: "security_prices",
                columns: new[] { "market", "symbol", "currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_security_prices_market_symbol_currency",
                table: "security_prices");

            migrationBuilder.DropColumn(
                name: "market",
                table: "security_prices");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "security_prices",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)",
                oldPrecision: 30,
                oldScale: 12);

            migrationBuilder.AlterColumn<string>(
                name: "symbol",
                table: "security_prices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "symbol",
                table: "investments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "investments",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,10)",
                oldPrecision: 30,
                oldScale: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_prices_symbol_currency",
                table: "security_prices",
                columns: new[] { "symbol", "currency" },
                unique: true);
        }
    }
}
