namespace FinGrow.Application.Validations.Reports;

using FinGrow.Application.Features.Reports.ExpenseBreakdown;
using FluentValidation;

public sealed class GetExpenseBreakdownValidator : AbstractValidator<GetExpenseBreakdownQuery>
{
    public GetExpenseBreakdownValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum();

        RuleFor(query => query)
            .Must(query => ReportPeriodRules.IsValid(query.From, query.To))
            .WithName("to")
            .WithMessage(ReportPeriodRules.Message);
    }
}
