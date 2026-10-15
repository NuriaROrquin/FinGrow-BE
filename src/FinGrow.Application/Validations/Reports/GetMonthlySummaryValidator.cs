namespace FinGrow.Application.Validations.Reports;

using FinGrow.Application.Features.Reports.MonthlySummary;
using FluentValidation;

public sealed class GetMonthlySummaryValidator : AbstractValidator<GetMonthlySummaryQuery>
{
    public GetMonthlySummaryValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum();

        RuleFor(query => query)
            .Must(query => ReportPeriodRules.IsValid(query.From, query.To))
            .WithName("to")
            .WithMessage(ReportPeriodRules.Message);
    }
}
