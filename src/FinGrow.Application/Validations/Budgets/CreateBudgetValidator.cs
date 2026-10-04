namespace FinGrow.Application.Validations.Budgets;

using FinGrow.Application.Features.Budgets.CreateBudget;
using FluentValidation;

public sealed class CreateBudgetValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(command => command.Year)
            .InclusiveBetween(BudgetPeriodRules.MinYear, BudgetPeriodRules.MaxYear)
            .WithMessage($"El anio del presupuesto tiene que estar entre {BudgetPeriodRules.MinYear} y {BudgetPeriodRules.MaxYear}.");

        RuleFor(command => command.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes del presupuesto tiene que estar entre 1 y 12.");

        RuleFor(command => command.Currency)
            .IsInEnum();

        RuleFor(command => command.Limits)
            .NotEmpty()
            .WithMessage("El presupuesto tiene que tener al menos un tope por categoria.");

        RuleFor(command => command.Limits)
            .Must(limits => limits.Select(limit => limit.Category).Distinct().Count() == limits.Count)
            .WithMessage("Cada categoria puede aparecer una sola vez en el presupuesto.")
            .When(command => command.Limits is not null);

        RuleForEach(command => command.Limits)
            .ChildRules(limit =>
            {
                limit.RuleFor(input => input.Category)
                    .IsInEnum()
                    .WithMessage("La categoria no es una categoria de gasto valida.");

                limit.RuleFor(input => input.Amount)
                    .GreaterThan(0m)
                    .WithMessage("El tope de una categoria tiene que ser mayor a cero.");
            })
            .When(command => command.Limits is not null);
    }
}
