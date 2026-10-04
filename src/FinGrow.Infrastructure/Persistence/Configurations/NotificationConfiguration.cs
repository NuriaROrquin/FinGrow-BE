namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", table =>
            table.HasCheckConstraint("ck_notifications_read_after_created", "read_at IS NULL OR read_at >= created_at"));

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(notification => notification.RecipientId).IsRequired();

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(Notification.MaxTitleLength)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasMaxLength(Notification.MaxBodyLength)
            .IsRequired();

        builder.Property(notification => notification.DeduplicationKey)
            .HasMaxLength(Notification.MaxDeduplicationKeyLength)
            .IsRequired();

        builder.Property(notification => notification.CreatedAt).IsRequired();
        builder.Property(notification => notification.ReadAt);

        builder.Ignore(notification => notification.Recipient);
        builder.Ignore(notification => notification.IsRead);

        builder.HasIndex(notification => new { notification.RecipientType, notification.RecipientId, notification.CreatedAt });

        builder.HasIndex(notification => new
        {
            notification.RecipientType,
            notification.RecipientId,
            notification.Type,
            notification.DeduplicationKey,
            notification.CreatedAt,
        }).HasDatabaseName("ix_notifications_deduplication");
    }
}
