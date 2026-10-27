namespace FinGrow.Api.UnitTests.Reports;

using FinGrow.Infrastructure.Reports;

/// <summary>
/// Los ejes del PDF tienen que marcar lo mismo que los de recharts en el panel: de 0 a un tope
/// redondo, en cuatro pasos iguales.
/// </summary>
public class ChartScaleTests
{
    private static readonly decimal[] Quarters = { 0m, 25m, 50m, 75m, 100m };

    [Theory]
    [InlineData(5, 8)]
    [InlineData(8, 8)]
    [InlineData(100, 100)]
    [InlineData(60, 60)]
    [InlineData(90, 100)]
    [InlineData(133.3, 140)]
    [InlineData(3, 4)]
    [InlineData(0, 4)]
    public void The_axis_goes_from_zero_to_a_round_top_in_four_equal_steps(double highest, double expectedTop)
    {
        var ticks = ChartScale.Ticks(0m, null, (decimal)highest);

        ticks.Count.ShouldBe(5);
        ticks[0].ShouldBe(0m);
        ticks[^1].ShouldBe((decimal)expectedTop);
        ticks.Zip(ticks.Skip(1), (lower, upper) => upper - lower).Distinct().ShouldHaveSingleItem();
    }

    [Fact]
    public void A_fixed_top_like_the_health_score_marks_every_quarter()
    {
        var ticks = ChartScale.Ticks(0m, 100m, 37m);

        ticks.ShouldBe(Quarters);
    }
}
