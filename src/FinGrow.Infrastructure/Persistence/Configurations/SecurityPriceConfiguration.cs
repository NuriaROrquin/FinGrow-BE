namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class SecurityPriceConfiguration : IEntityTypeConfiguration<SecurityPrice>
{
    public void Configure(EntityTypeBuilder<SecurityPrice> builder)
    {
        builder.ToTable("security_prices", table =>
            table.HasCheckConstraint("ck_security_prices_unit_price_positive", "unit_price > 0"));

        builder.HasKey(price => price.Id);

        builder.Property(price => price.Id).ValueGeneratedNever();

        builder.Property(price => price.Market)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(price => price.Symbol)
            .HasMaxLength(Investment.MaxFundNameLength)
            .IsRequired();

        builder.Property(price => price.Currency)
            .HasConversion<string>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(price => price.UnitPrice)
            .HasPrecision(30, 12)
            .IsRequired();

        builder.Property(price => price.PricedOn).IsRequired();

        builder.Property(price => price.Source)
            .HasMaxLength(SecurityPrice.MaxSourceLength)
            .IsRequired();

        builder.Property(price => price.UpdatedAt).IsRequired();

        builder.HasIndex(price => new { price.Market, price.Symbol, price.Currency }).IsUnique();
    }
}
