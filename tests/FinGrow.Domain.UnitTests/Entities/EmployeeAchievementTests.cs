namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;

public class EmployeeAchievementTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Unlocking_announces_the_achievement_and_leaves_it_to_celebrate()
    {
        var achievement = EmployeeAchievement.Unlock(EmployeeId, AchievementCode.GoalAchieved, Now, Now);

        achievement.EmployeeId.ShouldBe(EmployeeId);
        achievement.Code.ShouldBe(AchievementCode.GoalAchieved);
        achievement.AchievedAt.ShouldBe(Now);
        achievement.CreatedAt.ShouldBe(Now);
        achievement.IsCelebrated.ShouldBeFalse();

        var unlocked = achievement.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<AchievementUnlocked>();
        unlocked.EmployeeAchievementId.ShouldBe(achievement.Id);
        unlocked.EmployeeId.ShouldBe(EmployeeId);
        unlocked.Code.ShouldBe(AchievementCode.GoalAchieved);
        unlocked.OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void A_monthly_achievement_keeps_the_month_close_as_its_date()
    {
        var monthClose = new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero);

        var achievement = EmployeeAchievement.Unlock(EmployeeId, AchievementCode.Saver, monthClose, Now);

        achievement.AchievedAt.ShouldBe(monthClose);
        achievement.CreatedAt.ShouldBe(Now);
        achievement.DequeueDomainEvents().ShouldHaveSingleItem().OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void A_backfilled_achievement_is_not_announced_but_waits_to_be_celebrated()
    {
        var achievedAt = Now.AddMonths(-4);

        var achievement = EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstTransaction, achievedAt, Now);

        achievement.AchievedAt.ShouldBe(achievedAt);
        achievement.IsCelebrated.ShouldBeFalse();
        achievement.DequeueDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void A_recovered_achievement_is_born_celebrated_and_silent()
    {
        var achievedAt = Now.AddDays(-2);

        var achievement = EmployeeAchievement.Recover(EmployeeId, AchievementCode.FirstGoal, achievedAt, Now);

        achievement.AchievedAt.ShouldBe(achievedAt);
        achievement.CelebratedAt.ShouldBe(Now);
        achievement.DequeueDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Celebrating_twice_keeps_the_first_date()
    {
        var achievement = EmployeeAchievement.Unlock(EmployeeId, AchievementCode.FirstBudget, Now, Now);

        achievement.MarkCelebrated(Now.AddMinutes(1));
        achievement.MarkCelebrated(Now.AddMinutes(5));

        achievement.CelebratedAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public void An_achievement_cannot_be_celebrated_before_it_was_granted()
    {
        var achievement = EmployeeAchievement.Unlock(EmployeeId, AchievementCode.FirstBudget, Now, Now);

        Should.Throw<DomainException>(() => achievement.MarkCelebrated(Now.AddSeconds(-1)));
    }

    [Fact]
    public void An_achievement_cannot_be_recorded_before_its_condition_was_met()
    {
        Should.Throw<DomainException>(() =>
            EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstGoal, Now.AddSeconds(1), Now));
    }

    [Fact]
    public void An_achievement_always_belongs_to_an_employee_and_to_the_catalog()
    {
        Should.Throw<DomainException>(() =>
            EmployeeAchievement.Unlock(Guid.Empty, AchievementCode.FirstGoal, Now, Now));
        Should.Throw<DomainException>(() =>
            EmployeeAchievement.Unlock(EmployeeId, (AchievementCode)999, Now, Now));
    }
}
