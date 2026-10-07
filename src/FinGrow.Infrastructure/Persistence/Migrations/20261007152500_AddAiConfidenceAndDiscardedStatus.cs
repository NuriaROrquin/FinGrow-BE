using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConfidenceAndDiscardedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ai_confidence",
                table: "transactions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ai_model",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_transactions_ai_confidence_range",
                table: "transactions",
                sql: "ai_confidence IS NULL OR (ai_confidence >= 0 AND ai_confidence <= 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_transactions_ai_confidence_range",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ai_confidence",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ai_model",
                table: "transactions");
        }
    }
}
