namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MonthlyReportDeliveryConfiguration : IEntityTypeConfiguration<MonthlyReportDelivery>
{
    public void Configure(EntityTypeBuilder<MonthlyReportDelivery> builder)
    {
        builder.ToTable("monthly_report_deliveries", table =>
            table.HasCheckConstraint(
                "ck_monthly_report_deliveries_period_starts_on_day_one",
                "extract(day from period_start) = 1"));

        builder.HasKey(delivery => delivery.Id);

        builder.Property(delivery => delivery.PeriodStart).IsRequired();
        builder.Property(delivery => delivery.CreatedAt).IsRequired();
        builder.Property(delivery => delivery.QueuedAt).IsRequired();

        builder.Ignore(delivery => delivery.Period);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(delivery => delivery.CompanyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_monthly_report_deliveries_companies_company_id");

        // Restrict: el correo es la prueba de que el reporte de ese mes ya salió. Nadie borra
        // outbound_emails hoy; si algún día se limpian, esta FK obliga a decidir qué pasa con el envío.
        builder.HasOne<OutboundEmail>()
            .WithMany()
            .HasForeignKey(delivery => delivery.OutboundEmailId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_monthly_report_deliveries_outbound_emails_outbound_email_id");

        builder.HasIndex(delivery => new { delivery.CompanyId, delivery.PeriodStart })
            .IsUnique()
            .HasDatabaseName("ix_monthly_report_deliveries_company_id_period_start");

        builder.HasIndex(delivery => delivery.OutboundEmailId)
            .HasDatabaseName("ix_monthly_report_deliveries_outbound_email_id");
    }
}
