namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class IntegrationLinkCodeConfiguration : IEntityTypeConfiguration<IntegrationLinkCode>
{
    public void Configure(EntityTypeBuilder<IntegrationLinkCode> builder)
    {
        builder.ToTable("integration_link_codes");

        builder.HasKey(linkCode => linkCode.Id);

        builder.Property(linkCode => linkCode.Provider)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(linkCode => linkCode.CodeHash)
            .HasMaxLength(IntegrationLinkCode.HashLength)
            .IsRequired();

        builder.Property(linkCode => linkCode.ExpiresAt).IsRequired();
        builder.Property(linkCode => linkCode.CreatedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(linkCode => linkCode.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(linkCode => new { linkCode.Provider, linkCode.CodeHash }).IsUnique();
        builder.HasIndex(linkCode => linkCode.EmployeeId);
    }
}
