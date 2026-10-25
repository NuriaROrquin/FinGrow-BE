namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinGrow.Infrastructure.Persistence.Protection;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .HasMaxLength(Company.MaxNameLength)
            .IsRequired();

        builder.Property(company => company.TaxId)
            .HasColumnName("tax_id")
            .HasConversion(taxId => taxId.Value, value => TaxId.From(value))
            .HasMaxLength(TaxId.Length)
            .IsRequired();

        builder.Property(company => company.Industry)
            .HasMaxLength(Company.MaxIndustryLength);

        builder.Property(company => company.PhoneNumber)
            .HasMaxLength(Company.MaxPhoneNumberLength);

        builder.Property(company => company.FiscalAddress)
            .HasMaxLength(Company.MaxFiscalAddressLength);

        builder.Property(company => company.FoundedYear);

        builder.Property(company => company.DefaultCurrency)
            .HasConversion<string>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(company => company.Theme)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(company => company.Language)
            .HasConversion<string>()
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(company => company.FiscalYearStartMonth)
            .IsRequired();

        builder.Property(company => company.CreatedAt).IsRequired();
        builder.Property(company => company.UpdatedAt).IsRequired();

        builder.HasIndex(company => company.TaxId).IsUnique();

        builder.HasOne(company => company.User)
            .WithOne()
            .HasForeignKey<Company>(company => company.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(company => company.User).AutoInclude();

        // La coleccion se expone como IReadOnlyCollection; EF escribe sobre la lista privada.
        builder.Metadata
            .FindNavigation(nameof(Company.Departments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
