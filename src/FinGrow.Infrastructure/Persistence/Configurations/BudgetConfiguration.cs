namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(budget => budget.Id);

        builder.Property(budget => budget.Period)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(budget => budget.PeriodStart).IsRequired();
        builder.Property(budget => budget.CreatedAt).IsRequired();
        builder.Property(budget => budget.UpdatedAt).IsRequired();

        // Calculadas a partir de otros campos: viven en el dominio, no en una columna.
        builder.Ignore(budget => budget.PeriodEnd);
        builder.Ignore(budget => budget.Currency);

        builder.HasOne(budget => budget.Employee)
            .WithMany()
            .HasForeignKey(budget => budget.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(budget => budget.Limits)
            .WithOne()
            .HasForeignKey(limit => limit.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sin sus topes un presupuesto no dice nada: se cargan siempre con el.
        builder.Navigation(budget => budget.Limits)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        // Uno por empleado y periodo. El tipo entra porque el anual y el de enero arrancan el mismo dia.
        builder.HasIndex(budget => new { budget.EmployeeId, budget.Period, budget.PeriodStart })
            .IsUnique();
    }
}
