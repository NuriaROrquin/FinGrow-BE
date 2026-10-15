namespace FinGrow.Application.Validations.Reports;

using FinGrow.Application.Features.Reports.FinancialInsights;
using FluentValidation;

public sealed class GetFinancialInsightsValidator : AbstractValidator<GetFinancialInsightsQuery>
{
    public GetFinancialInsightsValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum();

        RuleFor(query => query)
            .Must(query => ReportPeriodRules.IsValid(query.From, query.To))
            .WithName("to")
            .WithMessage(ReportPeriodRules.Message);
    }
}
