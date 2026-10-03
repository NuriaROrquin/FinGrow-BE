namespace FinGrow.Application.Features.Transactions.GetMonthlyIncomeExpenses;

using System.Globalization;
using Common;
using Domain.Enums;
using FluentValidation;
using Interfaces;
using MediatR;

public sealed record GetMonthlyIncomeExpensesQuery(Currency Currency) : IRequest<Result<MonthlyIncomeExpensesResponse>>;

public sealed record MonthlyIncomeExpenseItem(
    string Month,
    decimal TotalIncome,
    decimal TotalExpense,
    Currency Currency);

public sealed record MonthlyIncomeExpensesResponse(IReadOnlyList<MonthlyIncomeExpenseItem> Items);

public sealed class GetMonthlyIncomeExpensesQueryValidator : AbstractValidator<GetMonthlyIncomeExpensesQuery>
{
    public GetMonthlyIncomeExpensesQueryValidator() =>
        RuleFor(query => query.Currency)
            .IsInEnum()
            .WithMessage("La moneda no esta soportada.");
}

internal sealed class GetMonthlyIncomeExpensesQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetMonthlyIncomeExpensesQuery, Result<MonthlyIncomeExpensesResponse>>
{
    public async Task<Result<MonthlyIncomeExpensesResponse>> Handle(
        GetMonthlyIncomeExpensesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<MonthlyIncomeExpensesResponse>(
                Error.Unauthorized("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar los movimientos."));
        }

        var (from, to) = TransactionChartPeriod.LastSixCalendarMonths(dateTimeProvider);
        var totals = await transactionReadRepository.GetMonthlyIncomeExpensesAsync(
            employeeId, request.Currency, from, to, cancellationToken);
        var totalsByMonth = totals.ToDictionary(
            total => (total.Year, total.Month),
            total => (total.TotalIncome, total.TotalExpense));

        var items = Enumerable.Range(0, 6)
            .Select(offset => from.AddMonths(offset))
            .Select(month =>
            {
                var totalsForMonth = totalsByMonth.GetValueOrDefault((month.Year, month.Month));
                return new MonthlyIncomeExpenseItem(
                    month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    totalsForMonth.TotalIncome,
                    totalsForMonth.TotalExpense,
                    request.Currency);
            })
            .ToList();

        return Result.Success(new MonthlyIncomeExpensesResponse(items));
    }
}
