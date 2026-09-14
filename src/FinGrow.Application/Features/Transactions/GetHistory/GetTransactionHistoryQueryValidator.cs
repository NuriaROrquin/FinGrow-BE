namespace FinGrow.Application.Features.Transactions.GetHistory;

using FluentValidation;

public sealed class GetTransactionHistoryQueryValidator : AbstractValidator<GetTransactionHistoryQuery>
{
    public GetTransactionHistoryQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Type)
            .Must(type => string.IsNullOrWhiteSpace(type)
                || type.Trim().Equals("ingreso", StringComparison.OrdinalIgnoreCase)
                || type.Trim().Equals("gasto", StringComparison.OrdinalIgnoreCase)
                || type.Trim().Equals("income", StringComparison.OrdinalIgnoreCase)
                || type.Trim().Equals("expense", StringComparison.OrdinalIgnoreCase))
            .WithMessage("El tipo debe ser 'ingreso' o 'gasto'.");
    }
}