using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_channel_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_channel_settings", x => x.id);
                    table.CheckConstraint("ck_notification_channel_settings_configurable_channel", "channel <> 'InApp'");
                    table.ForeignKey(
                        name: "fk_notification_channel_settings_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.CheckConstraint("ck_notifications_read_after_created", "read_at IS NULL OR read_at >= created_at");
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_settings_employee_id_channel",
                table: "notification_channel_settings",
                columns: new[] { "employee_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_deduplication",
                table: "notifications",
                columns: new[] { "recipient_type", "recipient_id", "type", "deduplication_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_type_recipient_id_created_at",
                table: "notifications",
                columns: new[] { "recipient_type", "recipient_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_channel_settings");

            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}
