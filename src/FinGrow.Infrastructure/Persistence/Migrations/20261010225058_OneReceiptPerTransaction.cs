using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneReceiptPerTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transaction_receipts_transaction_id",
                table: "transaction_receipts");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_receipts_transaction_id",
                table: "transaction_receipts",
                column: "transaction_id",
                unique: true,
                filter: "transaction_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transaction_receipts_transaction_id",
                table: "transaction_receipts");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_receipts_transaction_id",
                table: "transaction_receipts",
                column: "transaction_id");
        }
    }
}
