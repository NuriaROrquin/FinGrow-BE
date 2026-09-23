namespace FinGrow.Application.Validations.Goals;

using FinGrow.Application.Features.Goals.AddContribution;
using Interfaces;
using Domain.Entities;
using FluentValidation;

public sealed class AddGoalContributionValidator : AbstractValidator<AddGoalContributionCommand>
{
    public AddGoalContributionValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.Amount)
            .GreaterThan(0m)
            .WithMessage("El aporte tiene que ser mayor a cero.");

        RuleFor(command => command.Currency)
            .IsInEnum();

        RuleFor(command => command.ContributedOn)
            .Must(contributedOn => contributedOn <= DateOnly.FromDateTime(dateTimeProvider.UtcNow.UtcDateTime))
            .WithMessage("La fecha de un aporte no puede estar en el futuro.");

        RuleFor(command => command.Note)
            .MaximumLength(GoalContribution.MaxNoteLength)
            .WithMessage($"La nota de un aporte no puede superar los {GoalContribution.MaxNoteLength} caracteres.");
    }
}
