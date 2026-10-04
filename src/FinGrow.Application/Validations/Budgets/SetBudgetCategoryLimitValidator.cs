namespace FinGrow.Application.Validations.Budgets;

using FinGrow.Application.Features.Budgets.SetCategoryLimit;
using FluentValidation;

public sealed class SetBudgetCategoryLimitValidator : AbstractValidator<SetBudgetCategoryLimitCommand>
{
    public SetBudgetCategoryLimitValidator()
    {
        RuleFor(command => command.Year)
            .InclusiveBetween(BudgetPeriodRules.MinYear, BudgetPeriodRules.MaxYear)
            .WithMessage($"El anio del presupuesto tiene que estar entre {BudgetPeriodRules.MinYear} y {BudgetPeriodRules.MaxYear}.");

        RuleFor(command => command.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes del presupuesto tiene que estar entre 1 y 12.");

        RuleFor(command => command.Category)
            .IsInEnum()
            .WithMessage(BudgetLimitRules.InvalidCategoryMessage);

        RuleFor(command => command.Amount)
            .GreaterThan(0m)
            .WithMessage(BudgetLimitRules.MustBePositiveMessage)
            .LessThanOrEqualTo(BudgetLimitRules.MaxAmount)
            .WithMessage(BudgetLimitRules.TooLargeMessage);
    }
}
