namespace FinGrow.Application.Validations.Budgets;

using FinGrow.Application.Features.Budgets.Analysis;
using FluentValidation;

public sealed class GetBudgetAnalysisValidator : AbstractValidator<GetBudgetAnalysisQuery>
{
    public GetBudgetAnalysisValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum();
    }
}
