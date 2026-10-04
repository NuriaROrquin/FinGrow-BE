namespace FinGrow.Infrastructure.Persistence.Configurations;

using System.Linq.Expressions;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal static class PeriodMetricsConfigurationExtensions
{
    public static void OwnsPeriodMetrics<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, PeriodMetrics?>> navigation)
        where TEntity : class
    {
        builder.OwnsOne(navigation, metrics =>
        {
            metrics.Property(value => value.ActiveEmployees).HasColumnName("active_employees").IsRequired();
            metrics.Property(value => value.ParticipatingEmployees).HasColumnName("participating_employees").IsRequired();
            metrics.Property(value => value.ConfirmedTransactions).HasColumnName("confirmed_transactions").IsRequired();
            metrics.Property(value => value.EmployeesWithBudget).HasColumnName("employees_with_budget").IsRequired();
            metrics.Property(value => value.EmployeesWithActiveGoal).HasColumnName("employees_with_active_goal").IsRequired();
            metrics.Property(value => value.GoalsAchieved).HasColumnName("goals_achieved").IsRequired();
            metrics.Property(value => value.EmployeesWithIntegration).HasColumnName("employees_with_integration").IsRequired();
            metrics.Ignore(value => value.ParticipationRate);
        });

        builder.Navigation(navigation).IsRequired();
    }
}
