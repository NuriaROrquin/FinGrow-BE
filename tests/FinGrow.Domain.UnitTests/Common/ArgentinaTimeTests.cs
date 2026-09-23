namespace FinGrow.Domain.UnitTests.Common;

using FinGrow.Domain.Common;

public class ArgentinaTimeTests
{
    [Fact]
    public void Late_at_night_in_argentina_is_still_the_same_day_although_utc_already_changed()
    {
        var lateNightInBuenosAires = new DateTimeOffset(2026, 9, 24, 2, 30, 0, TimeSpan.Zero);

        ArgentinaTime.DateOf(lateNightInBuenosAires).ShouldBe(new DateOnly(2026, 9, 23));
    }

    [Fact]
    public void Midnight_in_argentina_starts_the_next_day()
    {
        var midnightInBuenosAires = new DateTimeOffset(2026, 9, 24, 3, 0, 0, TimeSpan.Zero);

        ArgentinaTime.DateOf(midnightInBuenosAires).ShouldBe(new DateOnly(2026, 9, 24));
    }
}
