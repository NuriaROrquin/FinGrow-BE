namespace FinGrow.Application.Features.Transactions.GetHistory;

using FluentValidation;

public sealed class GetTransactionHistoryQueryValidator : AbstractValidator<GetTransactionHistoryQuery>
{
    public GetTransactionHistoryQueryValidator() =>
        RuleFor(query => query.Filters).SetValidator(new TransactionFiltersValidator());
}

public sealed class TransactionFiltersValidator : AbstractValidator<TransactionFilters>
{
    public TransactionFiltersValidator()
    {
        RuleFor(filters => filters.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(filters => filters.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(filters => filters.Type)
            .IsInEnum()
            .When(filters => filters.Type.HasValue)
            .WithMessage("El tipo debe ser 'ingreso' o 'gasto'.");

        RuleFor(filters => filters.Status)
            .IsInEnum()
            .When(filters => filters.Status.HasValue)
            .WithMessage("El estado debe ser 'pendiente' o 'confirmado'.");

        RuleFor(filters => filters.ExpenseCategory)
            .IsInEnum()
            .When(filters => filters.ExpenseCategory.HasValue)
            .WithMessage("La categoria de gasto no existe.");

        RuleFor(filters => filters.IncomeCategory)
            .IsInEnum()
            .When(filters => filters.IncomeCategory.HasValue)
            .WithMessage("La categoria de ingreso no existe.");

        RuleFor(filters => filters.PaymentMethod)
            .IsInEnum()
            .When(filters => filters.PaymentMethod.HasValue)
            .WithMessage("El medio de pago no existe.");

        RuleFor(filters => filters)
            .Must(filters => !filters.DateFrom.HasValue
                || !filters.DateTo.HasValue
                || filters.DateFrom <= filters.DateTo)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}