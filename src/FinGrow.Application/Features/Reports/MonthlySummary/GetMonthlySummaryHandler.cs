namespace FinGrow.Application.Features.Reports.MonthlySummary;

using Common;
using Interfaces;
using MediatR;

internal sealed class GetMonthlySummaryHandler(
    IMonthlySummaryReadRepository monthlySummaryReadRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetMonthlySummaryQuery, Result<MonthlySummaryResponse>>
{
    public async Task<Result<MonthlySummaryResponse>> Handle(GetMonthlySummaryQuery request, CancellationToken cancellationToken)
    {
        var totals = await monthlySummaryReadRepository.ListMonthlyTotalsAsync(
            request.EmployeeId,
            request.Currency,
            request.From,
            request.To,
            cancellationToken);
        var firstMovement = await monthlySummaryReadRepository.FindFirstMovementDateAsync(
            request.EmployeeId,
            request.Currency,
            cancellationToken);

        var months = MonthlySummaryBuilder.TrimToHistory(
            MonthlySummaryBuilder.Build(totals, ReportPeriod.Months(request.From, request.To)),
            firstMovement,
            dateTimeProvider.Today);
        var totalSavings = months.Sum(month => month.Savings);

        return Result.Success(new MonthlySummaryResponse(
            request.Currency,
            request.From,
            request.To,
            months,
            months.Sum(month => month.Income),
            months.Sum(month => month.Expenses),
            totalSavings,
            months.Count > 0 ? decimal.Round(totalSavings / months.Count, 2) : null,
            MonthlySummaryBuilder.Combine(months.Select(month => month.ExpensesByCategory)),
            MonthHighlights.From(months)));
    }
}
