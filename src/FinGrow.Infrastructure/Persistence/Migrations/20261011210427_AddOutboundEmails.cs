using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbound_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    text_body = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_emails", x => x.id);
                    table.CheckConstraint("ck_outbound_emails_attempts_not_negative", "attempts >= 0");
                    table.CheckConstraint("ck_outbound_emails_body_only_while_awaiting", "status IN ('Pending', 'Failed') OR (html_body IS NULL AND text_body IS NULL)");
                    table.CheckConstraint("ck_outbound_emails_sent_has_provider_id", "status <> 'Sent' OR (sent_at IS NOT NULL AND provider_message_id IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbound_emails_status_next_attempt_at",
                table: "outbound_emails",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbound_emails");
        }
    }
}
