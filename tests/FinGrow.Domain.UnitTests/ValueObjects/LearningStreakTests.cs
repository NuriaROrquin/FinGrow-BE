namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.ValueObjects;

public class LearningStreakTests
{
    // Miercoles 14/10/2026. La semana en curso va del lunes 12 al domingo 18.
    private static readonly DateOnly Today = new(2026, 10, 14);

    private static DateTimeOffset At(int month, int day, int hour = 12) =>
        new(2026, month, day, hour, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Without_lessons_the_streak_is_zero()
    {
        var streak = LearningStreak.From(Array.Empty<DateTimeOffset>(), Today);

        streak.Weeks.ShouldBe(0);
        streak.StudiedThisWeek.ShouldBeFalse();
    }

    [Fact]
    public void Consecutive_weeks_with_lessons_add_up()
    {
        var streak = LearningStreak.From(new[] { At(10, 13), At(10, 6), At(9, 30) }, Today);

        streak.Weeks.ShouldBe(3);
        streak.StudiedThisWeek.ShouldBeTrue();
    }

    [Fact]
    public void Several_lessons_in_the_same_week_count_once()
    {
        var streak = LearningStreak.From(new[] { At(10, 12), At(10, 14), At(10, 18) }, Today);

        streak.Weeks.ShouldBe(1);
    }

    [Fact]
    public void The_streak_stays_alive_while_the_current_week_has_no_lessons_yet()
    {
        var streak = LearningStreak.From(new[] { At(10, 6), At(9, 30) }, Today);

        streak.Weeks.ShouldBe(2);
        streak.StudiedThisWeek.ShouldBeFalse();
    }

    [Fact]
    public void A_whole_week_without_lessons_resets_the_streak()
    {
        var streak = LearningStreak.From(new[] { At(9, 30), At(9, 23) }, Today);

        streak.Weeks.ShouldBe(0);
        streak.StudiedThisWeek.ShouldBeFalse();
    }

    [Fact]
    public void A_gap_in_the_middle_only_counts_the_weeks_after_it()
    {
        var streak = LearningStreak.From(new[] { At(10, 13), At(9, 22) }, Today);

        streak.Weeks.ShouldBe(1);
    }

    [Fact]
    public void Sunday_belongs_to_the_week_that_started_on_monday()
    {
        var sunday = new DateOnly(2026, 10, 18);

        var streak = LearningStreak.From(new[] { At(10, 12) }, sunday);

        streak.Weeks.ShouldBe(1);
        streak.StudiedThisWeek.ShouldBeTrue();
    }

    [Fact]
    public void The_week_is_decided_by_Argentina_time_and_not_by_UTC()
    {
        // 23:30 del domingo 11/10 en Argentina ya es lunes 12/10 en UTC.
        var lateSunday = new DateTimeOffset(2026, 10, 11, 23, 30, 0, TimeSpan.FromHours(-3));

        var streak = LearningStreak.From(new[] { lateSunday.ToUniversalTime() }, Today);

        streak.Weeks.ShouldBe(1);
        streak.StudiedThisWeek.ShouldBeFalse();
    }
}
