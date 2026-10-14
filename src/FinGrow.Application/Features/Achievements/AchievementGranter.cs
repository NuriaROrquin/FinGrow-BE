namespace FinGrow.Application.Features.Achievements;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal sealed class AchievementGranter(
    IAchievementRepository achievements,
    IAchievementProgressReadRepository progress,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
{
    public async Task<IReadOnlyList<EmployeeAchievement>> GrantInstantAsync(
        Guid employeeId,
        IReadOnlyCollection<AchievementCode> codes,
        AchievementGrantMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);

        var missing = await MissingAsync(employeeId, codes, cancellationToken);

        if (missing.Count == 0)
        {
            return Array.Empty<EmployeeAchievement>();
        }

        var reached = await progress.ListReachedAsync(employeeId, missing, cancellationToken);

        return await GrantAsync(employeeId, reached, mode, achievedNow: mode == AchievementGrantMode.Live, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeAchievement>> GrantReachedAsync(
        Guid employeeId,
        IReadOnlyDictionary<AchievementCode, DateTimeOffset> reached,
        AchievementGrantMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reached);

        var missing = await MissingAsync(employeeId, reached.Keys.ToList(), cancellationToken);

        return await GrantAsync(
            employeeId,
            reached.Where(pair => missing.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value),
            mode,
            achievedNow: false,
            cancellationToken);
    }

    private async Task<List<AchievementCode>> MissingAsync(
        Guid employeeId,
        IReadOnlyCollection<AchievementCode> codes,
        CancellationToken cancellationToken)
    {
        if (codes.Count == 0)
        {
            return new List<AchievementCode>();
        }

        var earned = (await achievements.ListByEmployeeAsync(employeeId, cancellationToken))
            .Select(achievement => achievement.Code)
            .ToHashSet();

        return codes.Distinct().Where(code => !earned.Contains(code)).ToList();
    }

    private async Task<IReadOnlyList<EmployeeAchievement>> GrantAsync(
        Guid employeeId,
        IReadOnlyDictionary<AchievementCode, DateTimeOffset> reached,
        AchievementGrantMode mode,
        bool achievedNow,
        CancellationToken cancellationToken)
    {
        if (reached.Count == 0)
        {
            return Array.Empty<EmployeeAchievement>();
        }

        var now = clock.UtcNow;
        var granted = reached
            .OrderBy(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair =>
            {
                var achievedAt = achievedNow || pair.Value > now ? now : pair.Value;

                return mode switch
                {
                    AchievementGrantMode.Live => EmployeeAchievement.Unlock(employeeId, pair.Key, achievedAt, now),
                    AchievementGrantMode.Backfill => EmployeeAchievement.Backfill(employeeId, pair.Key, achievedAt, now),
                    AchievementGrantMode.Recover => EmployeeAchievement.Recover(employeeId, pair.Key, achievedAt, now),
                    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de otorgamiento desconocido."),
                };
            })
            .ToList();

        foreach (var achievement in granted)
        {
            achievements.Add(achievement);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return granted;
    }
}
