namespace FinGrow.Application.Features.Reports.ExpenseBreakdown;

using Common;
using Domain.Enums;
using Interfaces;
using MediatR;

internal sealed class GetExpenseBreakdownHandler(IMonthlySummaryReadRepository monthlySummaryReadRepository)
    : IRequestHandler<GetExpenseBreakdownQuery, Result<ExpenseBreakdownResponse>>
{
    public async Task<Result<ExpenseBreakdownResponse>> Handle(GetExpenseBreakdownQuery request, CancellationToken cancellationToken)
    {
        var (previousFrom, previousTo) = ReportPeriod.Previous(request.From, request.To);
        var firstMovement = await monthlySummaryReadRepository.FindFirstMovementDateAsync(
            request.EmployeeId,
            request.Currency,
            cancellationToken);
        var hasPreviousPeriod = firstMovement is not null && firstMovement.Value <= previousTo;

        var current = await SummarizeAsync(request.EmployeeId, request.Currency, request.From, request.To, cancellationToken);
        var previous = hasPreviousPeriod
            ? await SummarizeAsync(request.EmployeeId, request.Currency, previousFrom, previousTo, cancellationToken)
            : null;

        return Result.Success(new ExpenseBreakdownResponse(
            request.Currency,
            request.From,
            request.To,
            previousFrom,
            previousTo,
            hasPreviousPeriod,
            ExpenseBreakdownBuilder.Categories(current, previous),
            ExpenseBreakdownBuilder.Total(current.Consumption, previous?.Consumption),
            ExpenseBreakdownBuilder.Total(current.Savings, previous?.Savings),
            ExpenseBreakdownBuilder.Total(current.Total, previous?.Total)));
    }

    private async Task<PeriodExpenses> SummarizeAsync(
        Guid employeeId,
        Currency currency,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var totals = await monthlySummaryReadRepository.ListMonthlyTotalsAsync(employeeId, currency, from, to, cancellationToken);

        return ExpenseBreakdownBuilder.Summarize(totals, from, to);
    }
}
