namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NotificationTypeSettingConfiguration : IEntityTypeConfiguration<NotificationTypeSetting>
{
    public void Configure(EntityTypeBuilder<NotificationTypeSetting> builder)
    {
        builder.ToTable("notification_type_settings");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(setting => setting.IsEnabled).IsRequired();
        builder.Property(setting => setting.UpdatedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(setting => setting.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(setting => new { setting.EmployeeId, setting.Type }).IsUnique();
    }
}
