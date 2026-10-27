namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CompanyNotificationSettingConfiguration : IEntityTypeConfiguration<CompanyNotificationSetting>
{
    public void Configure(EntityTypeBuilder<CompanyNotificationSetting> builder)
    {
        // Prendida siempre sabe desde cuándo, y apagada no: lo usa el reporte mensual (HU-82).
        builder.ToTable("company_notification_settings", table =>
            table.HasCheckConstraint(
                "ck_company_notification_settings_enabled_at_matches_is_enabled",
                "(is_enabled AND enabled_at IS NOT NULL) OR (NOT is_enabled AND enabled_at IS NULL)"));
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(setting => setting.Cadence).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(setting => setting.Threshold).HasPrecision(5, 2);
        builder.Property(setting => setting.IsEnabled).HasDefaultValue(false).IsRequired();
        builder.Property(setting => setting.LastEvaluatedAt);
        builder.Property(setting => setting.EnabledAt);
        builder.Property(setting => setting.UpdatedAt).IsRequired();
        builder.HasOne<Company>().WithMany().HasForeignKey(setting => setting.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(setting => new { setting.CompanyId, setting.Type }).IsUnique();
    }
}
