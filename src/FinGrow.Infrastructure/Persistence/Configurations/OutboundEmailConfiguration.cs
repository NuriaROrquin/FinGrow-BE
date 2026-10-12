namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using FinGrow.Infrastructure.Persistence.Protection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OutboundEmailConfiguration : IEntityTypeConfiguration<OutboundEmail>
{
    public void Configure(EntityTypeBuilder<OutboundEmail> builder)
    {
        builder.ToTable("outbound_emails", table =>
        {
            table.HasCheckConstraint("ck_outbound_emails_attempts_not_negative", "attempts >= 0");
            table.HasCheckConstraint(
                "ck_outbound_emails_body_only_while_awaiting",
                "status IN ('Pending', 'Failed') OR (html_body IS NULL AND text_body IS NULL)");
            table.HasCheckConstraint(
                "ck_outbound_emails_sent_has_provider_id",
                "status <> 'Sent' OR (sent_at IS NOT NULL AND provider_message_id IS NOT NULL)");
        });

        builder.HasKey(email => email.Id);

        builder.Property(email => email.Recipient)
            .HasConversion(email => email.Value, value => Email.From(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(email => email.Subject)
            .HasMaxLength(OutboundEmail.MaxSubjectLength)
            .IsRequired();

        builder.Property(email => email.HtmlBody)
            .HasAnnotation(EncryptedStringConverter.Annotation, true);

        builder.Property(email => email.TextBody)
            .HasAnnotation(EncryptedStringConverter.Annotation, true);

        builder.Property(email => email.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(email => email.LastError)
            .HasMaxLength(OutboundEmail.MaxErrorLength);

        builder.Property(email => email.ProviderMessageId)
            .HasMaxLength(OutboundEmail.MaxProviderMessageIdLength);

        builder.Property(email => email.CreatedAt).IsRequired();

        builder.Ignore(email => email.IsAwaitingDelivery);

        builder.HasIndex(email => new { email.Status, email.NextAttemptAt });
    }
}
