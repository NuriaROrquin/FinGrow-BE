using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundEmailAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_outbound_emails_body_only_while_awaiting",
                table: "outbound_emails");

            migrationBuilder.AddColumn<string>(
                name: "attachment_content",
                table: "outbound_emails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attachment_file_name",
                table: "outbound_emails",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbound_emails_attachment_has_file_name",
                table: "outbound_emails",
                sql: "attachment_content IS NULL OR attachment_file_name IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbound_emails_body_only_while_awaiting",
                table: "outbound_emails",
                sql: "status IN ('Pending', 'Failed') OR (html_body IS NULL AND text_body IS NULL AND attachment_content IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_outbound_emails_attachment_has_file_name",
                table: "outbound_emails");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbound_emails_body_only_while_awaiting",
                table: "outbound_emails");

            migrationBuilder.DropColumn(
                name: "attachment_content",
                table: "outbound_emails");

            migrationBuilder.DropColumn(
                name: "attachment_file_name",
                table: "outbound_emails");

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbound_emails_body_only_while_awaiting",
                table: "outbound_emails",
                sql: "status IN ('Pending', 'Failed') OR (html_body IS NULL AND text_body IS NULL)");
        }
    }
}
