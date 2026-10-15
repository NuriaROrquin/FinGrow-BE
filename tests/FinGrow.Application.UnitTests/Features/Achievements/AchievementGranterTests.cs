namespace FinGrow.Application.UnitTests.Features.Achievements;

using FinGrow.Application.Features.Achievements;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;

public class AchievementGranterTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LongAgo = Now.AddMonths(-5);

    private readonly FakeAchievementRepository _achievements = new();
    private readonly FakeAchievementProgressReadRepository _progress = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private AchievementGranter Granter => new(_achievements, _progress, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_live_grant_dates_the_achievement_now_and_announces_it()
    {
        _progress.Reach(EmployeeId, AchievementCode.FirstTransaction, LongAgo);

        var granted = await Granter.GrantInstantAsync(
            EmployeeId,
            new[] { AchievementCode.FirstTransaction, AchievementCode.ReceiptScanned },
            AchievementGrantMode.Live);

        var achievement = granted.ShouldHaveSingleItem();
        achievement.Code.ShouldBe(AchievementCode.FirstTransaction);
        achievement.AchievedAt.ShouldBe(Now);
        achievement.IsCelebrated.ShouldBeFalse();
        achievement.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<AchievementUnlocked>();
        _achievements.Achievements.ShouldHaveSingleItem();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_achievement_already_earned_is_not_evaluated_again()
    {
        _achievements.Add(EmployeeAchievement.Recover(EmployeeId, AchievementCode.FirstGoal, LongAgo, LongAgo));
        _progress.Reach(EmployeeId, AchievementCode.FirstGoal, LongAgo);

        var granted = await Granter.GrantInstantAsync(EmployeeId, new[] { AchievementCode.FirstGoal }, AchievementGrantMode.Live);

        granted.ShouldBeEmpty();
        _progress.Requests.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_backfill_keeps_the_original_date_and_waits_to_be_celebrated_together()
    {
        _progress.Reach(EmployeeId, AchievementCode.FirstGoal, LongAgo);
        _progress.Reach(EmployeeId, AchievementCode.FirstTransaction, LongAgo.AddDays(-3));

        var granted = await Granter.GrantInstantAsync(
            EmployeeId,
            AchievementCatalog.InstantCodes,
            AchievementGrantMode.Backfill);

        granted.Select(achievement => achievement.Code)
            .ShouldBe(new[] { AchievementCode.FirstTransaction, AchievementCode.FirstGoal });
        granted.ShouldAllBe(achievement => !achievement.IsCelebrated);
        granted.ShouldAllBe(achievement => achievement.DequeueDomainEvents().Count == 0);
        granted[1].AchievedAt.ShouldBe(LongAgo);
    }

    [Fact]
    public async Task A_recovered_achievement_is_recorded_already_celebrated()
    {
        _progress.Reach(EmployeeId, AchievementCode.FirstBudget, Now.AddDays(-2));

        var granted = await Granter.GrantInstantAsync(
            EmployeeId,
            new[] { AchievementCode.FirstBudget },
            AchievementGrantMode.Recover);

        var achievement = granted.ShouldHaveSingleItem();
        achievement.AchievedAt.ShouldBe(Now.AddDays(-2));
        achievement.CelebratedAt.ShouldBe(Now);
        achievement.DequeueDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_reached_monthly_achievement_keeps_the_month_close_and_skips_what_was_earned()
    {
        var monthClose = new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero);
        _achievements.Add(EmployeeAchievement.Recover(EmployeeId, AchievementCode.BudgetKept, LongAgo, LongAgo));

        var granted = await Granter.GrantReachedAsync(
            EmployeeId,
            new Dictionary<AchievementCode, DateTimeOffset>
            {
                [AchievementCode.BudgetKept] = monthClose,
                [AchievementCode.Saver] = monthClose,
            },
            AchievementGrantMode.Live);

        var achievement = granted.ShouldHaveSingleItem();
        achievement.Code.ShouldBe(AchievementCode.Saver);
        achievement.AchievedAt.ShouldBe(monthClose);
        achievement.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<AchievementUnlocked>();
    }
}
