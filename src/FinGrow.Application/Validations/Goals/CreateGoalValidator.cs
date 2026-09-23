namespace FinGrow.Application.Validations.Goals;

using FinGrow.Application.Features.Goals.CreateGoal;
using Interfaces;
using Domain.Entities;
using FluentValidation;

public sealed class CreateGoalValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre de la meta es obligatorio.")
            .MaximumLength(Goal.MaxNameLength)
            .WithMessage($"El nombre de la meta no puede superar los {Goal.MaxNameLength} caracteres.");

        RuleFor(command => command.TargetAmount)
            .GreaterThan(0m)
            .WithMessage("El monto objetivo tiene que ser mayor a cero.");

        RuleFor(command => command.Currency)
            .IsInEnum();

        RuleFor(command => command.Deadline)
            .Must(deadline => deadline >= dateTimeProvider.Today)
            .WithMessage("La fecha limite de una meta no puede estar en el pasado.");
    }
}
