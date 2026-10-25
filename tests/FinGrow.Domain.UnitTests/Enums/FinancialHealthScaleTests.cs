namespace FinGrow.Domain.UnitTests.Enums;

using FinGrow.Domain.Enums;

public class FinancialHealthScaleTests
{
    [Theory]
    [InlineData(0, FinancialHealthBand.Critical)]
    [InlineData(39, FinancialHealthBand.Critical)]
    [InlineData(40, FinancialHealthBand.Fragile)]
    [InlineData(59, FinancialHealthBand.Fragile)]
    [InlineData(60, FinancialHealthBand.Stable)]
    [InlineData(79, FinancialHealthBand.Stable)]
    [InlineData(80, FinancialHealthBand.Healthy)]
    [InlineData(100, FinancialHealthBand.Healthy)]
    public void Each_score_falls_in_one_band(int score, FinancialHealthBand expected)
    {
        FinancialHealthScale.BandOf(score).ShouldBe(expected);
    }

    [Theory]
    [InlineData(FinancialHealthBand.Critical, true)]
    [InlineData(FinancialHealthBand.Fragile, true)]
    [InlineData(FinancialHealthBand.Stable, false)]
    [InlineData(FinancialHealthBand.Healthy, false)]
    public void Only_critical_and_fragile_bands_need_attention(FinancialHealthBand band, bool expected)
    {
        FinancialHealthScale.NeedsAttention(band).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void A_score_goes_from_zero_to_one_hundred(int score, bool expected)
    {
        FinancialHealthScale.IsValid(score).ShouldBe(expected);
    }
}
