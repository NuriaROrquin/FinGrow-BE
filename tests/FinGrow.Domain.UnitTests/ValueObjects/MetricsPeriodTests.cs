namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class MetricsPeriodTests
{
    [Fact]
    public void A_period_covers_a_whole_calendar_month()
    {
        var period = MetricsPeriod.Of(2028, 2);

        period.Start.ShouldBe(new DateOnly(2028, 2, 1));
        period.End.ShouldBe(new DateOnly(2028, 2, 29));
        period.Contains(new DateOnly(2028, 2, 29)).ShouldBeTrue();
        period.Contains(new DateOnly(2028, 3, 1)).ShouldBeFalse();
        period.ToString().ShouldBe("2028-02");
    }

    [Fact]
    public void Month_of_any_day_normalizes_to_the_first_day()
    {
        MetricsPeriod.MonthOf(new DateOnly(2026, 9, 27)).ShouldBe(MetricsPeriod.Of(2026, 9));
    }

    [Theory]
    [InlineData(2026, 9, 1, 2026, 8)]
    [InlineData(2026, 9, 30, 2026, 8)]
    [InlineData(2026, 1, 15, 2025, 12)]
    public void The_last_closed_month_is_the_one_before_today(int year, int month, int day, int expectedYear, int expectedMonth)
    {
        var lastClosed = MetricsPeriod.LastClosedMonth(new DateOnly(year, month, day));

        lastClosed.ShouldBe(MetricsPeriod.Of(expectedYear, expectedMonth));
        lastClosed.IsClosed(new DateOnly(year, month, day)).ShouldBeTrue();
    }

    [Fact]
    public void The_current_month_is_not_closed()
    {
        MetricsPeriod.Of(2026, 9).IsClosed(new DateOnly(2026, 9, 30)).ShouldBeFalse();
    }

    [Fact]
    public void Until_walks_every_month_up_to_and_including_the_last_one()
    {
        var periods = MetricsPeriod.Of(2025, 11).Until(MetricsPeriod.Of(2026, 2)).ToList();

        periods.ShouldBe(new[]
        {
            MetricsPeriod.Of(2025, 11),
            MetricsPeriod.Of(2025, 12),
            MetricsPeriod.Of(2026, 1),
            MetricsPeriod.Of(2026, 2),
        });
    }

    [Fact]
    public void Until_yields_nothing_when_the_start_is_after_the_end()
    {
        MetricsPeriod.Of(2026, 9).Until(MetricsPeriod.Of(2026, 8)).ShouldBeEmpty();
    }

    [Fact]
    public void A_period_can_only_start_on_the_first_day_of_a_month()
    {
        Should.Throw<DomainException>(() => MetricsPeriod.FromStart(new DateOnly(2026, 9, 2)));
        MetricsPeriod.FromStart(new DateOnly(2026, 9, 1)).ShouldBe(MetricsPeriod.Of(2026, 9));
    }

    [Fact]
    public void Instants_are_argentina_midnights_expressed_in_utc()
    {
        var period = MetricsPeriod.Of(2026, 8);

        period.StartInstant.ShouldBe(new DateTimeOffset(2026, 8, 1, 3, 0, 0, TimeSpan.Zero));
        period.EndInstantExclusive.ShouldBe(new DateTimeOffset(2026, 9, 1, 3, 0, 0, TimeSpan.Zero));
        period.StartInstant.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Two_periods_of_the_same_month_are_equal()
    {
        MetricsPeriod.Of(2026, 8).ShouldBe(MetricsPeriod.MonthOf(new DateOnly(2026, 8, 20)));
        MetricsPeriod.Of(2026, 8).GetHashCode().ShouldBe(MetricsPeriod.Of(2026, 8).GetHashCode());
        MetricsPeriod.Of(2026, 8).ShouldNotBe(MetricsPeriod.Of(2026, 9));
    }
}
