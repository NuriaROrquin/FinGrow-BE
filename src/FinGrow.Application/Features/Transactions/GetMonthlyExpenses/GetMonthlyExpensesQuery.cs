namespace FinGrow.Application.Features.Transactions.GetMonthlyExpenses;

using System.Globalization;
using Common;
using Domain.Enums;
using FluentValidation;
using Interfaces;
using MediatR;

public sealed record GetMonthlyExpensesQuery(Currency Currency) : IRequest<Result<MonthlyExpensesResponse>>;

public sealed record MonthlyExpenseItem(string Month, decimal TotalExpense, Currency Currency);

public sealed record MonthlyExpensesResponse(IReadOnlyList<MonthlyExpenseItem> Items);

public sealed class GetMonthlyExpensesQueryValidator : AbstractValidator<GetMonthlyExpensesQuery>
{
    public GetMonthlyExpensesQueryValidator() =>
        RuleFor(query => query.Currency)
            .IsInEnum()
            .WithMessage("La moneda no esta soportada.");
}

internal sealed class GetMonthlyExpensesQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetMonthlyExpensesQuery, Result<MonthlyExpensesResponse>>
{
    public async Task<Result<MonthlyExpensesResponse>> Handle(
        GetMonthlyExpensesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<MonthlyExpensesResponse>(
                Error.Unauthorized("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar los gastos."));
        }

        var (from, to) = TransactionChartPeriod.LastSixCalendarMonths(dateTimeProvider);
        var totals = await transactionReadRepository.GetMonthlyExpensesAsync(
            employeeId, request.Currency, from, to, cancellationToken);
        var totalsByMonth = totals.ToDictionary(total => (total.Year, total.Month), total => total.Total);

        var items = Enumerable.Range(0, 6)
            .Select(offset => from.AddMonths(offset))
            .Select(month => new MonthlyExpenseItem(
                month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                totalsByMonth.GetValueOrDefault((month.Year, month.Month)),
                request.Currency))
            .ToList();

        return Result.Success(new MonthlyExpensesResponse(items));
    }
}
