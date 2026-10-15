namespace FinGrow.Application.Features.Investments.GetPortfolioEvolution;

using Common;
using Domain.Entities;
using Domain.Repositories;
using Interfaces;
using MediatR;

internal sealed class GetPortfolioEvolutionHandler(
    IInvestmentRepository investmentRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetPortfolioEvolutionQuery, Result<PortfolioEvolutionResponse>>
{
    private const int MaxDailySpanInDays = 366;

    private const int DaysPerWeek = 7;

    public async Task<Result<PortfolioEvolutionResponse>> Handle(
        GetPortfolioEvolutionQuery request,
        CancellationToken cancellationToken)
    {
        var investments = (await investmentRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken))
            .Where(investment => investment.InvestedAmount.Currency == request.Currency)
            .ToList();

        if (investments.Count == 0)
        {
            return Result.Success(new PortfolioEvolutionResponse(
                request.Currency,
                PortfolioEvolutionInterval.Day,
                null,
                Array.Empty<PortfolioEvolutionPoint>()));
        }

        var today = dateTimeProvider.Today;
        var historyStartsOn = investments.Min(investment => investment.PurchasedOn);
        var rangeStart = StartOf(request.Range, today) ?? historyStartsOn;
        var from = rangeStart > historyStartsOn ? rangeStart : historyStartsOn;

        if (from > today)
        {
            from = today;
        }

        var interval = today.DayNumber - from.DayNumber > MaxDailySpanInDays
            ? PortfolioEvolutionInterval.Week
            : PortfolioEvolutionInterval.Day;

        var points = SampleDates(from, today, interval)
            .Select(date => PointOn(date, investments))
            .ToList();

        return Result.Success(new PortfolioEvolutionResponse(request.Currency, interval, historyStartsOn, points));
    }

    private static DateOnly? StartOf(PortfolioEvolutionRange range, DateOnly today) => range switch
    {
        PortfolioEvolutionRange.OneMonth => today.AddMonths(-1),
        PortfolioEvolutionRange.ThreeMonths => today.AddMonths(-3),
        PortfolioEvolutionRange.SixMonths => today.AddMonths(-6),
        PortfolioEvolutionRange.OneYear => today.AddYears(-1),
        _ => null
    };

    private static List<DateOnly> SampleDates(DateOnly from, DateOnly to, PortfolioEvolutionInterval interval)
    {
        var step = interval == PortfolioEvolutionInterval.Week ? DaysPerWeek : 1;
        var dates = new List<DateOnly>();

        for (var date = to; date > from; date = date.AddDays(-step))
        {
            dates.Add(date);
        }

        dates.Add(from);
        dates.Reverse();

        return dates;
    }

    private static PortfolioEvolutionPoint PointOn(DateOnly date, IEnumerable<Investment> investments)
    {
        var held = investments.Where(investment => investment.PurchasedOn <= date).ToList();

        return new PortfolioEvolutionPoint(
            date,
            held.Sum(investment => investment.ValueOn(date).Amount),
            held.Sum(investment => investment.InvestedAmount.Amount));
    }
}
