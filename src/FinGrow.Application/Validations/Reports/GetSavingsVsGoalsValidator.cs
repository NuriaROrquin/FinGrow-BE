namespace FinGrow.Application.Validations.Reports;

using FinGrow.Application.Features.Reports.SavingsVsGoals;
using FluentValidation;

public sealed class GetSavingsVsGoalsValidator : AbstractValidator<GetSavingsVsGoalsQuery>
{
    public GetSavingsVsGoalsValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum();

        RuleFor(query => query.Months)
            .InclusiveBetween(1, GetSavingsVsGoalsQuery.MaxMonths)
            .When(query => query.Months is not null)
            .WithMessage($"El periodo tiene que ser de entre 1 y {GetSavingsVsGoalsQuery.MaxMonths} meses.");
    }
}
