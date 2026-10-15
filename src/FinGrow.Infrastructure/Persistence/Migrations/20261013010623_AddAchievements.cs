using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinGrow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    condition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    evaluation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_major = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_achievements", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "employee_achievements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    achieved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    celebrated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_achievements", x => x.id);
                    table.CheckConstraint("ck_employee_achievements_achieved_before_recorded", "achieved_at <= created_at");
                    table.CheckConstraint("ck_employee_achievements_celebrated_after_recorded", "celebrated_at IS NULL OR celebrated_at >= created_at");
                    table.ForeignKey(
                        name: "fk_employee_achievements_achievements_code",
                        column: x => x.code,
                        principalTable: "achievements",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_achievements_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "achievements",
                columns: new[] { "code", "condition", "description", "evaluation", "is_major", "name" },
                values: new object[,]
                {
                    { "BudgetKept", "Cerró un mes con actividad suficiente y con presupuesto mensual, sin ninguna categoría por encima de su tope.", "Cerraste un mes sin pasarte de ningún tope de tu presupuesto.", "MonthlyClose", true, "Dentro del plan" },
                    { "ChannelLinked", "Vinculó al menos una integración: Gmail, Mercado Pago, Telegram, WhatsApp o InvertirOnline.", "Vinculaste tu primera integración.", "Instant", false, "Conectado" },
                    { "ConsistentThreeMonths", "Cerró 3 meses consecutivos con actividad suficiente: al menos 10 movimientos confirmados y algún ingreso en cada uno.", "Registraste tus finanzas tres meses seguidos.", "MonthlyClose", true, "Constancia" },
                    { "FirstBudget", "Tiene al menos un presupuesto con un tope por categoría.", "Armaste tu primer presupuesto.", "Instant", false, "Con plan" },
                    { "FirstCourse", "Completó todas las lecciones de al menos un curso publicado.", "Terminaste tu primer curso.", "Instant", false, "Aprendiz" },
                    { "FirstGoal", "Creó al menos una meta de ahorro.", "Creaste tu primera meta de ahorro.", "Instant", false, "Con objetivo" },
                    { "FirstInvestment", "Tiene al menos una inversión, cargada a mano o importada de InvertirOnline.", "Registraste tu primera inversión.", "Instant", false, "Inversor" },
                    { "FirstTransaction", "Tiene al menos un movimiento confirmado, cargado a mano o por cualquier integración.", "Confirmaste tu primer movimiento.", "Instant", false, "Primer paso" },
                    { "GoalAchieved", "Tiene al menos una meta alcanzada.", "Llegaste al objetivo de una meta de ahorro.", "Instant", true, "Meta cumplida" },
                    { "ReceiptScanned", "Tiene al menos un movimiento confirmado que entró por la lectura de un comprobante.", "Registraste un movimiento sacándole una foto al comprobante.", "Instant", false, "Sin papeles" },
                    { "Saver", "Cerró un mes con actividad suficiente en el que lo que no gastó fue al menos el 10% de sus ingresos. Lo destinado a ahorro_inversion cuenta como ahorro, igual que en el puntaje de salud financiera.", "Ahorraste al menos el 10% de tus ingresos en un mes.", "MonthlyClose", true, "Ahorrador" },
                    { "ThreeGoalsAchieved", "Tiene al menos 3 metas alcanzadas.", "Cumpliste tres metas de ahorro.", "Instant", true, "Imparable" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_achievements_code",
                table: "employee_achievements",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_employee_achievements_employee_id_code",
                table: "employee_achievements",
                columns: new[] { "employee_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_achievements");

            migrationBuilder.DropTable(
                name: "achievements");
        }
    }
}
