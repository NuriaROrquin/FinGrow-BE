using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // Backfill: todo movimiento que ya existia antes de que existiera el estado
                // estaba, por definicion, contando. Nace confirmado.
                defaultValue: "Confirmed");

            // El default era solo para rellenar las filas viejas. Se saca para que un INSERT
            // que se olvide del estado falle en vez de inventar uno.
            migrationBuilder.Sql("ALTER TABLE transactions ALTER COLUMN status DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_employee_id_status",
                table: "transactions",
                columns: new[] { "employee_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transactions_employee_id_status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "transactions");
        }
    }
}
