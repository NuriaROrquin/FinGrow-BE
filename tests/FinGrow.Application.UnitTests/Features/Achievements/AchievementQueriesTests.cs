namespace FinGrow.Application.UnitTests.Features.Achievements;

using FinGrow.Application.Features.Achievements.CelebrateAchievements;
using FinGrow.Application.Features.Achievements.ListAchievements;
using FinGrow.Application.Features.Achievements.ListUncelebratedAchievements;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class AchievementQueriesTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly Guid OtherEmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeAchievementRepository _achievements = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task The_list_shows_the_whole_catalog_with_the_date_of_what_was_earned()
    {
        _achievements.Add(EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstGoal, Now.AddDays(-30), Now));
        _achievements.Add(EmployeeAchievement.Backfill(OtherEmployeeId, AchievementCode.Saver, Now.AddDays(-30), Now));

        var result = await new ListAchievementsHandler(_achievements)
            .Handle(new ListAchievementsQuery(EmployeeId), CancellationToken.None);

        result.Value.Count.ShouldBe(AchievementCatalog.All.Count);
        result.Value.Single(achievement => achievement.Code == AchievementCode.FirstGoal).AchievedAt.ShouldBe(Now.AddDays(-30));
        result.Value.Where(achievement => achievement.AchievedAt is not null).ShouldHaveSingleItem();
        result.Value.Single(achievement => achievement.Code == AchievementCode.Saver).IsMajor.ShouldBeTrue();
    }

    [Fact]
    public async Task Only_uncelebrated_achievements_are_left_for_the_front_in_the_order_they_were_reached()
    {
        _achievements.Add(EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstGoal, Now.AddDays(-2), Now));
        _achievements.Add(EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstTransaction, Now.AddDays(-9), Now));
        _achievements.Add(EmployeeAchievement.Recover(EmployeeId, AchievementCode.FirstBudget, Now.AddDays(-1), Now));

        var result = await new ListUncelebratedAchievementsHandler(_achievements)
            .Handle(new ListUncelebratedAchievementsQuery(EmployeeId), CancellationToken.None);

        result.Value.Select(achievement => achievement.Code)
            .ShouldBe(new[] { AchievementCode.FirstTransaction, AchievementCode.FirstGoal });
    }

    [Fact]
    public async Task Celebrating_marks_only_the_achievements_the_front_showed()
    {
        _achievements.Add(EmployeeAchievement.Unlock(EmployeeId, AchievementCode.FirstGoal, Now, Now));
        _achievements.Add(EmployeeAchievement.Unlock(EmployeeId, AchievementCode.FirstBudget, Now, Now));

        var result = await Celebrate(AchievementCode.FirstGoal);

        result.Value.Celebrated.ShouldBe(1);
        _achievements.Achievements.Single(achievement => achievement.Code == AchievementCode.FirstGoal).CelebratedAt.ShouldBe(Now.AddMinutes(1));
        _achievements.Achievements.Single(achievement => achievement.Code == AchievementCode.FirstBudget).IsCelebrated.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Celebrating_again_or_someone_elses_achievement_changes_nothing()
    {
        _achievements.Add(EmployeeAchievement.Unlock(OtherEmployeeId, AchievementCode.FirstGoal, Now, Now));
        _achievements.Add(EmployeeAchievement.Recover(EmployeeId, AchievementCode.FirstBudget, Now, Now));

        var result = await Celebrate(AchievementCode.FirstGoal, AchievementCode.FirstBudget);

        result.Value.Celebrated.ShouldBe(0);
        _achievements.Achievements.Single(achievement => achievement.EmployeeId == OtherEmployeeId).IsCelebrated.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public void Celebrating_needs_at_least_one_known_code()
    {
        var validator = new CelebrateAchievementsCommandValidator();

        validator.Validate(new CelebrateAchievementsCommand(EmployeeId, Array.Empty<AchievementCode>())).IsValid.ShouldBeFalse();
        validator.Validate(new CelebrateAchievementsCommand(EmployeeId, new[] { (AchievementCode)999 })).IsValid.ShouldBeFalse();
        validator.Validate(new CelebrateAchievementsCommand(EmployeeId, new[] { AchievementCode.Saver })).IsValid.ShouldBeTrue();
    }

    private Task<Application.Common.Result<CelebratedAchievementsResponse>> Celebrate(params AchievementCode[] codes) =>
        new CelebrateAchievementsHandler(_achievements, _unitOfWork, new FakeDateTimeProvider(Now.AddMinutes(1)))
            .Handle(new CelebrateAchievementsCommand(EmployeeId, codes), CancellationToken.None);
}
