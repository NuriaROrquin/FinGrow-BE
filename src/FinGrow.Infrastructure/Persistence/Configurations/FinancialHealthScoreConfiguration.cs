namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class FinancialHealthScoreConfiguration : IEntityTypeConfiguration<FinancialHealthScore>
{
    private const int FactorDigits = 5;
    private const int FactorDecimals = 2;

    public void Configure(EntityTypeBuilder<FinancialHealthScore> builder)
    {
        builder.ToTable("financial_health_scores", table =>
        {
            table.HasCheckConstraint("ck_financial_health_scores_period_starts_on_day_one", "extract(day from period_start) = 1");
            table.HasCheckConstraint("ck_financial_health_scores_score_range", "score between 0 and 100");
            table.HasCheckConstraint(
                "ck_financial_health_scores_factors_range",
                "savings_factor between 0 and 100 and cushion_factor between 0 and 100 "
                + "and (budget_factor is null or budget_factor between 0 and 100) "
                + "and (goals_factor is null or goals_factor between 0 and 100)");
        });

        builder.HasKey(score => score.Id);

        builder.Property(score => score.PeriodStart).IsRequired();
        builder.Property(score => score.Score).IsRequired();
        builder.Property(score => score.ComputedAt).IsRequired();

        builder.OwnsOne(score => score.Factors, factors =>
        {
            factors.Property(value => value.Savings).HasColumnName("savings_factor").HasPrecision(FactorDigits, FactorDecimals).IsRequired();
            factors.Property(value => value.Budget).HasColumnName("budget_factor").HasPrecision(FactorDigits, FactorDecimals);
            factors.Property(value => value.Goals).HasColumnName("goals_factor").HasPrecision(FactorDigits, FactorDecimals);
            factors.Property(value => value.Cushion).HasColumnName("cushion_factor").HasPrecision(FactorDigits, FactorDecimals).IsRequired();
            factors.Ignore(value => value.Score);
            factors.Ignore(value => value.Band);
        });

        builder.Navigation(score => score.Factors).IsRequired();

        builder.Ignore(score => score.Period);
        builder.Ignore(score => score.Band);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(score => score.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(score => new { score.EmployeeId, score.PeriodStart }).IsUnique();
    }
}
