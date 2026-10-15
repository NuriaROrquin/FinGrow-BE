namespace FinGrow.Domain.ValueObjects;

public readonly record struct FiscalYear(int StartYear, int StartMonth)
{
    public static FiscalYear From(DateOnly period, int startMonth)
    {
        if (startMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(startMonth));
        }

        return new FiscalYear(
            period.Month >= startMonth ? period.Year : period.Year - 1,
            startMonth);
    }
}
