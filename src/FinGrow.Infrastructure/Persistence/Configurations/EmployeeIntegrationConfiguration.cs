namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
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
