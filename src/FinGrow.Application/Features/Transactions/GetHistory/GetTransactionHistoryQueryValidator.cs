namespace FinGrow.Application.Features.Transactions.GetHistory;

using FinGrow.Domain.Enums;
using FluentValidation;

public sealed class GetTransactionHistoryQueryValidator : AbstractValidator<GetTransactionHistoryQuery>
{
    public GetTransactionHistoryQueryValidator()
    {
        RuleFor(query => query.Filters.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.Filters.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Filters.Type)
            .Must(type => !type.HasValue || Enum.IsDefined(type.Value))
            .WithMessage("El tipo de transaccion no es valido.");

        RuleForEach(query => query.Filters.Status)
            .Must(status => Enum.IsDefined(status))
            .WithMessage("El estado de la transaccion no es valido.");

        RuleFor(query => query.Filters.ExpenseCategory)
            .Must(category => !category.HasValue || Enum.IsDefined(category.Value))
            .WithMessage("La categoria de gasto no es valida.");

        RuleFor(query => query.Filters.IncomeCategory)
            .Must(category => !category.HasValue || Enum.IsDefined(category.Value))
            .WithMessage("La categoria de ingreso no es valida.");

        RuleFor(query => query.Filters)
            .Must(filters => !filters.DateFrom.HasValue
                || !filters.DateTo.HasValue
                || filters.DateFrom <= filters.DateTo)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}