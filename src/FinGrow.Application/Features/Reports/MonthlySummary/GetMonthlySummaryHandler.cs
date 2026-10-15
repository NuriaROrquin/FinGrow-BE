namespace FinGrow.Application.Features.Reports.MonthlySummary;

using Common;
using Interfaces;
using MediatR;

internal sealed class GetMonthlySummaryHandler(IMonthlySummaryReadRepository monthlySummaryReadRepository)
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

        var months = MonthlySummaryBuilder.Build(totals, ReportPeriod.Months(request.From, request.To));

        return Result.Success(new MonthlySummaryResponse(
            request.Currency,
            request.From,
            request.To,
            months,
            months.Sum(month => month.Income),
            months.Sum(month => month.Expenses),
            months.Sum(month => month.Savings),
            MonthlySummaryBuilder.Combine(months.Select(month => month.ExpensesByCategory))));
    }
}
