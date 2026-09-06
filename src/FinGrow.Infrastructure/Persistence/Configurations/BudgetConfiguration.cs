namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets", table =>
            table.HasCheckConstraint("ck_budgets_limit_positive", "limit_amount > 0"));

        builder.HasKey(budget => budget.Id);

        builder.Property(budget => budget.Category)
            .HasConversion(new ExpenseCategoryConverter())
            .HasMaxLength(ExpenseCategoryConverter.MaxLength)
            .IsRequired();

        builder.OwnsMoney(budget => budget.Limit, "limit_amount", "currency");

        builder.Property(budget => budget.Period)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(budget => budget.PeriodStart).IsRequired();
        builder.Property(budget => budget.CreatedAt).IsRequired();
        builder.Property(budget => budget.UpdatedAt).IsRequired();

        // Calculadas a partir de otros campos: viven en el dominio, no en una columna.
        builder.Ignore(budget => budget.PeriodEnd);

        builder.HasOne(budget => budget.Employee)
            .WithMany()
            .HasForeignKey(budget => budget.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un solo presupuesto por categoria y periodo: si no, "cuanto me queda" tiene dos respuestas.
        builder.HasIndex(budget => new { budget.EmployeeId, budget.Category, budget.PeriodStart })
            .IsUnique();
    }
}
