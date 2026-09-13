namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("investments", table =>
            table.HasCheckConstraint("ck_investments_invested_positive", "invested_amount > 0"));

        builder.HasKey(investment => investment.Id);

        builder.Property(investment => investment.AssetName)
            .HasMaxLength(Investment.MaxAssetNameLength)
            .IsRequired();

        builder.Property(investment => investment.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsMoney(investment => investment.InvestedAmount, "invested_amount", "invested_currency");

        builder.Property(investment => investment.PurchasedOn).IsRequired();
        builder.Property(investment => investment.CreatedAt).IsRequired();
        builder.Property(investment => investment.UpdatedAt).IsRequired();

        builder.Ignore(investment => investment.LatestValuation);
        builder.Ignore(investment => investment.CurrentValue);
        builder.Ignore(investment => investment.ValuedOn);
        builder.Ignore(investment => investment.ReturnAmount);
        builder.Ignore(investment => investment.ReturnPercentage);

        builder.HasOne(investment => investment.Employee)
            .WithMany()
            .HasForeignKey(investment => investment.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(investment => investment.Valuations)
            .WithOne()
            .HasForeignKey(valuation => valuation.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sin sus valuaciones una inversion no sabe cuanto vale: se cargan siempre con ella.
        builder.Navigation(investment => investment.Valuations)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(investment => new { investment.EmployeeId, investment.Type });
    }
}
