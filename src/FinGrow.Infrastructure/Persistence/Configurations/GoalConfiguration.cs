namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals", table =>
        {
            table.HasCheckConstraint("ck_goals_target_positive", "target_amount > 0");

            // Objetivo y progreso son dos importes distintos de la misma meta: si quedaran en
            // monedas diferentes, el porcentaje de avance dejaria de significar algo.
            table.HasCheckConstraint("ck_goals_same_currency", "target_currency = current_currency");
        });

        builder.HasKey(goal => goal.Id);

        builder.Property(goal => goal.Name)
            .HasMaxLength(Goal.MaxNameLength)
            .IsRequired();

        builder.OwnsMoney(goal => goal.TargetAmount, "target_amount", "target_currency");
        builder.OwnsMoney(goal => goal.CurrentAmount, "current_amount", "current_currency");

        builder.Property(goal => goal.Deadline).IsRequired();

        builder.Property(goal => goal.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(goal => goal.CreatedAt).IsRequired();
        builder.Property(goal => goal.UpdatedAt).IsRequired();

        builder.Ignore(goal => goal.ProgressPercentage);
        builder.Ignore(goal => goal.RemainingAmount);

        builder.HasOne(goal => goal.Employee)
            .WithMany()
            .HasForeignKey(goal => goal.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(goal => new { goal.EmployeeId, goal.Status });
    }
}
