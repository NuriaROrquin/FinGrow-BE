namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NotificationChannelSettingConfiguration : IEntityTypeConfiguration<NotificationChannelSetting>
{
    public void Configure(EntityTypeBuilder<NotificationChannelSetting> builder)
    {
        builder.ToTable("notification_channel_settings", table =>
            table.HasCheckConstraint("ck_notification_channel_settings_configurable_channel", "channel <> 'InApp'"));

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(setting => setting.IsEnabled).IsRequired();
        builder.Property(setting => setting.UpdatedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(setting => setting.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(setting => new { setting.EmployeeId, setting.Channel }).IsUnique();
    }
}
