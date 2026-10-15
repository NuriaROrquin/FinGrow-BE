namespace FinGrow.Api.Contracts;

using FinGrow.Domain.Enums;

public sealed record CelebrateAchievementsRequest(IReadOnlyCollection<AchievementCode> Codes);
