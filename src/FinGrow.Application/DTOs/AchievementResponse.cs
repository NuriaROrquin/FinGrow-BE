namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record AchievementResponse(
    AchievementCode Code,
    string Name,
    string Description,
    string Condition,
    bool IsMajor,
    DateTimeOffset? AchievedAt)
{
    public static AchievementResponse From(Achievement achievement, EmployeeAchievement? earned) => new(
        achievement.Code,
        achievement.Name,
        achievement.Description,
        achievement.Condition,
        achievement.IsMajor,
        earned?.AchievedAt);
}
