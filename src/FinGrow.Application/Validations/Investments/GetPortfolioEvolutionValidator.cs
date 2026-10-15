namespace FinGrow.Application.Validations.Investments;

using FinGrow.Application.Features.Investments.GetPortfolioEvolution;
using FluentValidation;

public sealed class GetPortfolioEvolutionValidator : AbstractValidator<GetPortfolioEvolutionQuery>
{
    public GetPortfolioEvolutionValidator()
    {
        RuleFor(query => query.Currency)
            .IsInEnum()
            .WithMessage("La moneda no esta soportada.");

        RuleFor(query => query.Range)
            .IsInEnum()
            .WithMessage("El rango no esta soportado: tiene que ser OneMonth, ThreeMonths, SixMonths, OneYear o All.");
    }
}
