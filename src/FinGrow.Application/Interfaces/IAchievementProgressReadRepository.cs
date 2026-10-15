namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Enums;

public interface IAchievementProgressReadRepository
{

    Task<IReadOnlyDictionary<AchievementCode, DateTimeOffset>> ListReachedAsync(
        Guid employeeId,
        IReadOnlyCollection<AchievementCode> codes,
        CancellationToken cancellationToken = default);
}
