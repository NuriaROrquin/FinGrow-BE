namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Una consulta por logro pedido, que devuelve cuando se cumplio por primera vez la condicion de
/// <see cref="AchievementCatalog"/>.
/// </summary>
internal sealed class AchievementProgressReadRepository(FinGrowDbContext dbContext) : IAchievementProgressReadRepository
{
    public async Task<IReadOnlyDictionary<AchievementCode, DateTimeOffset>> ListReachedAsync(
        Guid employeeId,
        IReadOnlyCollection<AchievementCode> codes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);

        var reached = new Dictionary<AchievementCode, DateTimeOffset>();

        foreach (var code in codes.Distinct())
        {
            if (await ReachedAtAsync(employeeId, code, cancellationToken) is { } reachedAt)
            {
                reached[code] = reachedAt;
            }
        }

        return reached;
    }

    private Task<DateTimeOffset?> ReachedAtAsync(Guid employeeId, AchievementCode code, CancellationToken cancellationToken) =>
        code switch
        {
            AchievementCode.FirstTransaction => ConfirmedTransactions(employeeId)
                .MinAsync(transaction => (DateTimeOffset?)transaction.CreatedAt, cancellationToken),
            AchievementCode.ReceiptScanned => ConfirmedTransactions(employeeId)
                .Where(transaction => transaction.Source == TransactionSource.ReceiptScan)
                .MinAsync(transaction => (DateTimeOffset?)transaction.CreatedAt, cancellationToken),
            AchievementCode.ChannelLinked => dbContext.EmployeeIntegrations
                .Where(integration => integration.EmployeeId == employeeId)
                .MinAsync(integration => (DateTimeOffset?)integration.LinkedAt, cancellationToken),
            AchievementCode.FirstBudget => dbContext.Budgets
                .Where(budget => budget.EmployeeId == employeeId)
                .SelectMany(budget => budget.Limits)
                .MinAsync(limit => (DateTimeOffset?)limit.CreatedAt, cancellationToken),
            AchievementCode.FirstGoal => dbContext.Goals
                .Where(goal => goal.EmployeeId == employeeId)
                .MinAsync(goal => (DateTimeOffset?)goal.CreatedAt, cancellationToken),
            AchievementCode.GoalAchieved => AchievedGoalDates(employeeId)
                .MinAsync(cancellationToken),
            AchievementCode.ThreeGoalsAchieved => AchievedGoalDates(employeeId)
                .OrderBy(achievedAt => achievedAt)
                .Skip(AchievementCatalog.GoalsForThreeGoalsAchieved - 1)
                .FirstOrDefaultAsync(cancellationToken),
            AchievementCode.FirstInvestment => dbContext.Investments
                .Where(investment => investment.EmployeeId == employeeId)
                .MinAsync(investment => (DateTimeOffset?)investment.CreatedAt, cancellationToken),
            AchievementCode.FirstCourse => FirstCourseCompletedAtAsync(employeeId, cancellationToken),
            _ => Task.FromResult<DateTimeOffset?>(null),
        };

    private IQueryable<Transaction> ConfirmedTransactions(Guid employeeId) =>
        dbContext.Transactions.Where(transaction =>
            transaction.EmployeeId == employeeId && transaction.Status == TransactionStatus.Confirmed);

    private IQueryable<DateTimeOffset?> AchievedGoalDates(Guid employeeId) =>
        dbContext.Goals
            .Where(goal => goal.EmployeeId == employeeId && goal.Status == GoalStatus.Achieved && goal.AchievedAt != null)
            .Select(goal => goal.AchievedAt);

    /// <summary>Un curso se termina con su ultima leccion pendiente: esa es la fecha del logro.</summary>
    private Task<DateTimeOffset?> FirstCourseCompletedAtAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var completedByCourse = dbContext.LessonCompletions
            .Where(completion => completion.EmployeeId == employeeId)
            .Join(
                dbContext.Lessons,
                completion => completion.LessonId,
                lesson => lesson.Id,
                (completion, lesson) => new { lesson.CourseId, completion.CompletedAt })
            .GroupBy(completion => completion.CourseId)
            .Select(group => new
            {
                CourseId = group.Key,
                Completed = group.Count(),
                LastCompletedAt = group.Max(completion => completion.CompletedAt),
            });

        return dbContext.Courses
            .IgnoreAutoIncludes()
            .Where(course => course.PublishedAt != null)
            .Join(
                completedByCourse,
                course => course.Id,
                progress => progress.CourseId,
                (course, progress) => new { course.Id, progress.Completed, progress.LastCompletedAt })
            .Where(course => course.Completed == dbContext.Lessons.Count(lesson => lesson.CourseId == course.Id))
            .MinAsync(course => (DateTimeOffset?)course.LastCompletedAt, cancellationToken);
    }
}
