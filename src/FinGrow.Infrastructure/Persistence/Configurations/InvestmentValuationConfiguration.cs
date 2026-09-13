namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class InvestmentValuationConfiguration : IEntityTypeConfiguration<InvestmentValuation>
{
    public void Configure(EntityTypeBuilder<InvestmentValuation> builder)
    {
        builder.ToTable("investment_valuations");

        builder.HasKey(valuation => valuation.Id);

        builder.OwnsMoney(valuation => valuation.Value, "value", "currency");

        builder.Property(valuation => valuation.ValuedOn).IsRequired();

        builder.Property(valuation => valuation.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(valuation => valuation.CreatedAt).IsRequired();

        builder.HasIndex(valuation => new { valuation.InvestmentId, valuation.ValuedOn });
    }
}
