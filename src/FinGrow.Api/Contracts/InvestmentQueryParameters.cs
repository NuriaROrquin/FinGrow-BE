namespace FinGrow.Api.Contracts;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Investments.ListInvestments;
using FinGrow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

public sealed class InvestmentQueryParameters
{
    [FromQuery]
    public int PageNumber { get; init; } = 1;

    [FromQuery]
    public int PageSize { get; init; } = 10;

    [FromQuery]
    public string? Search { get; init; }

    [FromQuery(Name = "types")]
    public IReadOnlyList<InvestmentType>? Types { get; init; }

    [FromQuery(Name = "currencies")]
    public IReadOnlyList<Currency>? Currencies { get; init; }

    [FromQuery]
    public DateOnly? PurchasedFrom { get; init; }

    [FromQuery]
    public DateOnly? PurchasedTo { get; init; }

    [FromQuery]
    public decimal? MinInvested { get; init; }

    [FromQuery]
    public decimal? MaxInvested { get; init; }

    [FromQuery]
    public bool? Quoted { get; init; }

    [FromQuery]
    public InvestmentPerformance? Performance { get; init; }

    [FromQuery]
    public InvestmentSortField SortBy { get; init; } = InvestmentSortField.PurchasedOn;

    [FromQuery]
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    public InvestmentFilters ToFilters() => new(
        PageNumber,
        PageSize,
        Search,
        Types,
        Currencies,
        PurchasedFrom,
        PurchasedTo,
        MinInvested,
        MaxInvested,
        Quoted,
        Performance,
        SortBy,
        SortDirection);
}
