namespace FinGrow.Infrastructure.Persistence;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed partial class FinGrowDbContext
{
    /// <summary>Nombre del filtro global que aísla los datos por empresa.</summary>
    public const string TenantFilter = "Tenant";

    /// <summary>
    /// Entidades que no pertenecen a ninguna empresa (catálogo educativo, cotizaciones, corridas de
    /// trabajos). Toda entidad nueva tiene que aplicar el filtro de empresa o figurar acá a propósito:
    /// un test rompe el build si no hace ninguna de las dos cosas.
    /// </summary>
    public static IReadOnlySet<Type> GlobalEntityTypes { get; } = new HashSet<Type>
    {
        typeof(Article),
        typeof(Course),
        typeof(Lesson),
        typeof(SecurityPrice),
        typeof(JobRun),
        typeof(OutboundEmail),
    };

    // Sin sesion (webhooks, trabajos programados, login, seeders) no hay empresa a la que acotar.
    // Con sesion el filtro siempre aplica: un token sin company_id no ve ninguna fila.
    private bool IsUnscoped => !_currentUser.IsAuthenticated;

    private Guid? CurrentCompanyId => _currentUser.CompanyId;

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().HasQueryFilter(
            TenantFilter, company => IsUnscoped || company.Id == CurrentCompanyId);

        modelBuilder.Entity<Employee>().HasQueryFilter(
            TenantFilter, employee => IsUnscoped || employee.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<Department>().HasQueryFilter(
            TenantFilter, department => IsUnscoped || department.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<DepartmentMetricsSnapshot>().HasQueryFilter(
            TenantFilter, snapshot => IsUnscoped || snapshot.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<CompanyMetricsSnapshot>().HasQueryFilter(
            TenantFilter, snapshot => IsUnscoped || snapshot.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<RefreshToken>().HasQueryFilter(
            TenantFilter, token => IsUnscoped || token.CompanyId == CurrentCompanyId);

        // Lo que cuelga de un empleado hereda su empresa: el filtro de Employees se aplica solo dentro
        // de estas subconsultas, así que alcanza con preguntar si el dueño es visible.
        modelBuilder.Entity<Transaction>().HasQueryFilter(
            TenantFilter, transaction => IsUnscoped || Employees.Any(employee => employee.Id == transaction.EmployeeId));

        modelBuilder.Entity<TransactionReceipt>().HasQueryFilter(
            TenantFilter, receipt => IsUnscoped || Employees.Any(employee => employee.Id == receipt.EmployeeId));

        modelBuilder.Entity<Budget>().HasQueryFilter(
            TenantFilter, budget => IsUnscoped || Employees.Any(employee => employee.Id == budget.EmployeeId));

        modelBuilder.Entity<Goal>().HasQueryFilter(
            TenantFilter, goal => IsUnscoped || Employees.Any(employee => employee.Id == goal.EmployeeId));

        modelBuilder.Entity<Investment>().HasQueryFilter(
            TenantFilter, investment => IsUnscoped || Employees.Any(employee => employee.Id == investment.EmployeeId));

        modelBuilder.Entity<EmployeeIntegration>().HasQueryFilter(
            TenantFilter, integration => IsUnscoped || Employees.Any(employee => employee.Id == integration.EmployeeId));

        modelBuilder.Entity<IntegrationLinkCode>().HasQueryFilter(
            TenantFilter, linkCode => IsUnscoped || Employees.Any(employee => employee.Id == linkCode.EmployeeId));

        modelBuilder.Entity<EmployeeDepartmentAssignment>().HasQueryFilter(
            TenantFilter, assignment => IsUnscoped || Employees.Any(employee => employee.Id == assignment.EmployeeId));

        modelBuilder.Entity<CourseRating>().HasQueryFilter(
            TenantFilter, rating => IsUnscoped || Employees.Any(employee => employee.Id == rating.EmployeeId));

        modelBuilder.Entity<LessonCompletion>().HasQueryFilter(
            TenantFilter, completion => IsUnscoped || Employees.Any(employee => employee.Id == completion.EmployeeId));

        modelBuilder.Entity<NotificationChannelSetting>().HasQueryFilter(
            TenantFilter, setting => IsUnscoped || Employees.Any(employee => employee.Id == setting.EmployeeId));

        modelBuilder.Entity<FinancialHealthScore>().HasQueryFilter(
            TenantFilter, score => IsUnscoped || Employees.Any(employee => employee.Id == score.EmployeeId));

        // El destinatario es polimórfico y no tiene FK, así que se resuelve según su tipo.
        modelBuilder.Entity<Notification>().HasQueryFilter(
            TenantFilter,
            notification => IsUnscoped
                || (notification.RecipientType == NotificationRecipientType.Company
                    ? notification.RecipientId == CurrentCompanyId
                    : Employees.Any(employee => employee.Id == notification.RecipientId)));

        modelBuilder.Entity<GoalContribution>().HasQueryFilter(
            TenantFilter, contribution => IsUnscoped || Goals.Any(goal => goal.Id == contribution.GoalId));

        modelBuilder.Entity<InvestmentValuation>().HasQueryFilter(
            TenantFilter, valuation => IsUnscoped || Investments.Any(investment => investment.Id == valuation.InvestmentId));

        modelBuilder.Entity<BudgetCategoryLimit>().HasQueryFilter(
            TenantFilter, limit => IsUnscoped || Budgets.Any(budget => budget.Id == limit.BudgetId));
    }
}
