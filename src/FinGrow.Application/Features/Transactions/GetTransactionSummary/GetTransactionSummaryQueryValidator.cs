namespace FinGrow.Application.Features.Transactions.GetTransactionSummary;

using FluentValidation;

public sealed class GetTransactionSummaryQueryValidator : AbstractValidator<GetTransactionSummaryQuery>
{
    public GetTransactionSummaryQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue
                || !query.ToDate.HasValue
                || query.FromDate <= query.ToDate)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}
