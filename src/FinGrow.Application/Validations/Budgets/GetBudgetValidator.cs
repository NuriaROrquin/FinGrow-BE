namespace FinGrow.Application.Validations.Budgets;

using FinGrow.Application.Features.Budgets.GetBudget;
using FluentValidation;

public sealed class GetBudgetValidator : AbstractValidator<GetBudgetQuery>
{
    public GetBudgetValidator()
    {
        RuleFor(query => query.Year)
            .InclusiveBetween(BudgetPeriodRules.MinYear, BudgetPeriodRules.MaxYear)
            .WithMessage($"El anio del presupuesto tiene que estar entre {BudgetPeriodRules.MinYear} y {BudgetPeriodRules.MaxYear}.");

        RuleFor(query => query.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes del presupuesto tiene que estar entre 1 y 12.");
    }
}
