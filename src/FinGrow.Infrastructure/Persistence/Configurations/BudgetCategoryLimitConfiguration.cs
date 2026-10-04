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

        // El id lo genera el dominio. Si EF lo creyera generado por la base, un tope agregado a un
        // presupuesto ya guardado llegaria con id y lo trataria como existente: UPDATE en vez de INSERT.
        builder.Property(limit => limit.Id).ValueGeneratedNever();

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
