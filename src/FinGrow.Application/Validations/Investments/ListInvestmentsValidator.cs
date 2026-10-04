namespace FinGrow.Application.Validations.Investments;

using FinGrow.Application.Features.Investments.ListInvestments;
using Domain.Entities;
using FluentValidation;

public sealed class ListInvestmentsValidator : AbstractValidator<ListInvestmentsQuery>
{
    public ListInvestmentsValidator() =>
        RuleFor(query => query.Filters).SetValidator(new InvestmentFiltersValidator());
}

public sealed class InvestmentFiltersValidator : AbstractValidator<InvestmentFilters>
{
    public const int MaxPageSize = 100;

    public InvestmentFiltersValidator()
    {
        RuleFor(filters => filters.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La pagina tiene que ser 1 o mayor.");

        RuleFor(filters => filters.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"La cantidad por pagina tiene que estar entre 1 y {MaxPageSize}.");

        RuleFor(filters => filters.Search)
            .MaximumLength(Investment.MaxAssetNameLength)
            .WithMessage($"La busqueda no puede superar los {Investment.MaxAssetNameLength} caracteres.");

        RuleForEach(filters => filters.Types)
            .IsInEnum()
            .WithMessage("Uno de los tipos de activo no esta soportado.");

        RuleForEach(filters => filters.Currencies)
            .IsInEnum()
            .WithMessage("Una de las monedas no esta soportada.");

        RuleFor(filters => filters)
            .Must(filters => filters.PurchasedFrom is null
                || filters.PurchasedTo is null
                || filters.PurchasedFrom <= filters.PurchasedTo)
            .WithMessage("La fecha de compra desde no puede ser posterior a la fecha hasta.");

        RuleFor(filters => filters.MinInvested)
            .GreaterThanOrEqualTo(0m)
            .When(filters => filters.MinInvested.HasValue)
            .WithMessage("El capital minimo no puede ser negativo.");

        RuleFor(filters => filters.MaxInvested)
            .GreaterThanOrEqualTo(0m)
            .When(filters => filters.MaxInvested.HasValue)
            .WithMessage("El capital maximo no puede ser negativo.");

        RuleFor(filters => filters)
            .Must(filters => filters.MinInvested is null
                || filters.MaxInvested is null
                || filters.MinInvested <= filters.MaxInvested)
            .WithMessage("El capital minimo no puede ser mayor que el maximo.");

        RuleFor(filters => filters.Performance)
            .IsInEnum()
            .When(filters => filters.Performance.HasValue)
            .WithMessage("El resultado tiene que ser ganancia o perdida.");

        RuleFor(filters => filters.SortBy)
            .IsInEnum()
            .WithMessage("El orden pedido no existe.");

        RuleFor(filters => filters.SortDirection)
            .IsInEnum()
            .WithMessage("La direccion del orden tiene que ser ascendente o descendente.");
    }
}
