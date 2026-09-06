namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .HasMaxLength(Company.MaxNameLength)
            .IsRequired();

        // TaxId y Email son value objects de una sola propiedad: se aplastan a una columna.
        builder.Property(company => company.TaxId)
            .HasColumnName("tax_id")
            .HasConversion(taxId => taxId.Value, value => TaxId.From(value))
            .HasMaxLength(TaxId.Length)
            .IsRequired();

        builder.Property(company => company.Email)
            .HasColumnName("email")
            .HasConversion(email => email.Value, value => Email.From(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(company => company.PasswordHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.DefaultCurrency)
            .HasConversion<string>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(company => company.IsActive).IsRequired();
        builder.Property(company => company.CreatedAt).IsRequired();
        builder.Property(company => company.UpdatedAt).IsRequired();

        builder.HasIndex(company => company.TaxId).IsUnique();
        builder.HasIndex(company => company.Email).IsUnique();

        // La coleccion se expone como IReadOnlyCollection; EF escribe sobre la lista privada.
        builder.Metadata
            .FindNavigation(nameof(Company.Departments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
