namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record AchievementUnlocked(
    Guid EmployeeAchievementId,
    Guid EmployeeId,
    AchievementCode Code,
    DateTimeOffset OccurredAt) : IDomainEvent;
