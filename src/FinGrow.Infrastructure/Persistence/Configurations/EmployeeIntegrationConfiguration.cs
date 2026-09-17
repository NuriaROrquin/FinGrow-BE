namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Infrastructure.Persistence.Protection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EmployeeIntegrationConfiguration : IEntityTypeConfiguration<EmployeeIntegration>
{
    public void Configure(EntityTypeBuilder<EmployeeIntegration> builder)
    {
        builder.ToTable("employee_integrations");

        builder.HasKey(integration => integration.Id);

        builder.Property(integration => integration.Provider)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(integration => integration.ExternalAccountId)
            .HasMaxLength(EmployeeIntegration.MaxExternalAccountIdLength)
            .IsRequired();

        builder.Property(integration => integration.LinkedAt).IsRequired();

        builder.OwnsOne(integration => integration.Grant, grant =>
        {
            grant.Property(value => value.AccessToken)
                .HasColumnName("oauth_access_token")
                .HasAnnotation(EncryptedStringConverter.Annotation, true)
                .IsRequired();

            grant.Property(value => value.RefreshToken)
                .HasColumnName("oauth_refresh_token")
                .HasAnnotation(EncryptedStringConverter.Annotation, true);

            grant.Property(value => value.ExpiresAt)
                .HasColumnName("oauth_expires_at")
                .IsRequired();
        });

        builder.Property(integration => integration.LastSyncedAt);
        builder.Property(integration => integration.CreatedAt).IsRequired();
        builder.Property(integration => integration.UpdatedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(integration => integration.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(integration => new { integration.EmployeeId, integration.Provider }).IsUnique();
        builder.HasIndex(integration => new { integration.Provider, integration.ExternalAccountId }).IsUnique();
    }
}
