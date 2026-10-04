namespace FinGrow.Application.Features.Transactions.GetExpensesByCategory;

using Common;
using Domain.Enums;
using FluentValidation;
using Interfaces;
using MediatR;
using Transactions;

public sealed record GetExpensesByCategoryQuery(
    Currency Currency,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<Result<ExpensesByCategoryResponse>>;

public sealed record ExpenseCategoryItem(string Category, decimal TotalExpense, Currency Currency);

public sealed record ExpensesByCategoryResponse(IReadOnlyList<ExpenseCategoryItem> Items);

public sealed class GetExpensesByCategoryQueryValidator : AbstractValidator<GetExpensesByCategoryQuery>
{
    public GetExpensesByCategoryQueryValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum()
            .WithMessage("La moneda no esta soportada.");

        RuleFor(query => query)
            .Must(query => !query.DateFrom.HasValue
                || !query.DateTo.HasValue
                || query.DateFrom <= query.DateTo)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}

internal sealed class GetExpensesByCategoryQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetExpensesByCategoryQuery, Result<ExpensesByCategoryResponse>>
{
    public async Task<Result<ExpensesByCategoryResponse>> Handle(
        GetExpensesByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<ExpensesByCategoryResponse>(
                Error.Unauthorized("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar los gastos."));
        }

        var defaultPeriod = TransactionChartPeriod.LastSixCalendarMonths(dateTimeProvider);
        var from = request.DateFrom ?? defaultPeriod.From;
        var to = request.DateTo ?? defaultPeriod.To;
        var totals = await transactionReadRepository.GetExpensesByCategoryAsync(
            employeeId, request.Currency, from, to, cancellationToken);

        var items = totals
            .Where(total => total.Total > 0m)
            .Select(total => new ExpenseCategoryItem(
                total.Category.ToString(),
                total.Total,
                request.Currency))
            .ToList();

        return Result.Success(new ExpensesByCategoryResponse(items));
    }
}
