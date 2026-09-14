namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals", table =>
            table.HasCheckConstraint("ck_goals_target_positive", "target_amount > 0"));

        builder.HasKey(goal => goal.Id);

        builder.Property(goal => goal.Name)
            .HasMaxLength(Goal.MaxNameLength)
            .IsRequired();

        builder.OwnsMoney(goal => goal.TargetAmount, "target_amount", "target_currency");

        builder.Property(goal => goal.Deadline).IsRequired();

        builder.Property(goal => goal.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(goal => goal.CreatedAt).IsRequired();
        builder.Property(goal => goal.UpdatedAt).IsRequired();

        builder.Ignore(goal => goal.CurrentAmount);
        builder.Ignore(goal => goal.ProgressPercentage);
        builder.Ignore(goal => goal.RemainingAmount);

        builder.HasOne(goal => goal.Employee)
            .WithMany()
            .HasForeignKey(goal => goal.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(goal => goal.Contributions)
            .WithOne()
            .HasForeignKey(contribution => contribution.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sin sus aportes una meta no sabe cuanto lleva: se cargan siempre con ella.
        builder.Navigation(goal => goal.Contributions)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(goal => new { goal.EmployeeId, goal.Status });
    }
}
