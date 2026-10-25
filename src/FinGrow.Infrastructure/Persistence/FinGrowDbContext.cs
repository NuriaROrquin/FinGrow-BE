namespace FinGrow.Infrastructure.Persistence;

using System.Reflection;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Infrastructure.Persistence.Protection;
using Microsoft.EntityFrameworkCore;

public sealed partial class FinGrowDbContext : DbContext, IUnitOfWork
{
    private readonly ISecretProtector _protector;
    private readonly ICurrentUser _currentUser;

    public FinGrowDbContext(
        DbContextOptions<FinGrowDbContext> options,
        ISecretProtector protector,
        ICurrentUser currentUser) : base(options)
    {
        _protector = protector;
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeDepartmentAssignment> EmployeeDepartmentAssignments => Set<EmployeeDepartmentAssignment>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<TransactionReceipt> TransactionReceipts => Set<TransactionReceipt>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<BudgetCategoryLimit> BudgetCategoryLimits => Set<BudgetCategoryLimit>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<GoalContribution> GoalContributions => Set<GoalContribution>();

    public DbSet<Investment> Investments => Set<Investment>();

    public DbSet<InvestmentValuation> InvestmentValuations => Set<InvestmentValuation>();

    public DbSet<SecurityPrice> SecurityPrices => Set<SecurityPrice>();

    public DbSet<EmployeeIntegration> EmployeeIntegrations => Set<EmployeeIntegration>();

    public DbSet<IntegrationLinkCode> IntegrationLinkCodes => Set<IntegrationLinkCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<JobRun> JobRuns => Set<JobRun>();

    public DbSet<CompanyMetricsSnapshot> CompanyMetricsSnapshots => Set<CompanyMetricsSnapshot>();

    public DbSet<DepartmentMetricsSnapshot> DepartmentMetricsSnapshots => Set<DepartmentMetricsSnapshot>();

    public DbSet<FinancialHealthScore> FinancialHealthScores => Set<FinancialHealthScore>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<Article> Articles => Set<Article>();

    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();

    public DbSet<CourseRating> CourseRatings => Set<CourseRating>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationChannelSetting> NotificationChannelSettings => Set<NotificationChannelSetting>();

    public DbSet<NotificationTypeSetting> NotificationTypeSettings => Set<NotificationTypeSetting>();

    public DbSet<CompanyNotificationSetting> CompanyNotificationSettings => Set<CompanyNotificationSetting>();

    public DbSet<OutboundEmail> OutboundEmails => Set<OutboundEmail>();

    public DbSet<Achievement> Achievements => Set<Achievement>();

    public DbSet<EmployeeAchievement> EmployeeAchievements => Set<EmployeeAchievement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ApplyTenantFilters(modelBuilder);

        var encrypted = new EncryptedStringConverter(_protector);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entity => entity.GetProperties())
                     .Where(property => property.FindAnnotation(EncryptedStringConverter.Annotation)?.Value is true))
        {
            property.SetValueConverter(encrypted);
        }

        // Va al final a proposito: renombra lo que quedo con nombre por convencion, despues de
        // que cada configuracion haya puesto los nombres que si le importan.
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
