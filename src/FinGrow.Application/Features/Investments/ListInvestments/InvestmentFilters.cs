namespace FinGrow.Application.Features.Investments.ListInvestments;

using Common;
using Domain.Enums;

public sealed record InvestmentFilters(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    IReadOnlyList<InvestmentType>? Types = null,
    IReadOnlyList<Currency>? Currencies = null,
    DateOnly? PurchasedFrom = null,
    DateOnly? PurchasedTo = null,
    decimal? MinInvested = null,
    decimal? MaxInvested = null,
    bool? Quoted = null,
    InvestmentPerformance? Performance = null,
    InvestmentSortField SortBy = InvestmentSortField.PurchasedOn,
    SortDirection SortDirection = SortDirection.Descending);
