namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BudgetCategoryLimitConfiguration : IEntityTypeConfiguration<BudgetCategoryLimit>
{
    public void Configure(EntityTypeBuilder<BudgetCategoryLimit> builder)
    {
        builder.ToTable("budget_category_limits", table =>
            table.HasCheckConstraint("ck_budget_category_limits_limit_positive", "limit_amount > 0"));

        builder.HasKey(limit => limit.Id);

        builder.Property(limit => limit.Category)
            .HasConversion(new ExpenseCategoryConverter())
            .HasMaxLength(ExpenseCategoryConverter.MaxLength)
            .IsRequired();

        builder.OwnsMoney(limit => limit.Limit, "limit_amount", "currency");

        builder.Property(limit => limit.CreatedAt).IsRequired();
        builder.Property(limit => limit.UpdatedAt).IsRequired();

        builder.HasIndex(limit => new { limit.BudgetId, limit.Category }).IsUnique();
    }
}
