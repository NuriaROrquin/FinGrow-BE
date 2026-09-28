namespace FinGrow.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Investments.ListInvestments;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class InvestmentReadRepository(FinGrowDbContext dbContext) : IInvestmentReadRepository
{
    private static readonly Expression<Func<Investment, decimal>> LatestValue = investment => investment.Valuations
        .OrderByDescending(valuation => valuation.ValuedOn)
        .ThenByDescending(valuation => valuation.CreatedAt)
        .Select(valuation => valuation.Value.Amount)
        .FirstOrDefault();

    private static readonly Expression<Func<Investment, decimal>> ReturnRatio = investment =>
        (investment.Valuations
            .OrderByDescending(valuation => valuation.ValuedOn)
            .ThenByDescending(valuation => valuation.CreatedAt)
            .Select(valuation => valuation.Value.Amount)
            .FirstOrDefault() - investment.InvestedAmount.Amount) / investment.InvestedAmount.Amount;

    private static readonly Expression<Func<Investment, bool>> IsGaining = investment => investment.Valuations
        .OrderByDescending(valuation => valuation.ValuedOn)
        .ThenByDescending(valuation => valuation.CreatedAt)
        .Select(valuation => valuation.Value.Amount)
        .FirstOrDefault() > investment.InvestedAmount.Amount;

    private static readonly Expression<Func<Investment, bool>> IsLosing = investment => investment.Valuations
        .OrderByDescending(valuation => valuation.ValuedOn)
        .ThenByDescending(valuation => valuation.CreatedAt)
        .Select(valuation => valuation.Value.Amount)
        .FirstOrDefault() < investment.InvestedAmount.Amount;

    private static readonly Expression<Func<Investment, bool>> IsQuoted = investment =>
        investment.Valuations.Any(valuation => valuation.Source == ValuationSource.Feed);

    private static readonly Expression<Func<Investment, bool>> IsUnquoted = investment =>
        !investment.Valuations.Any(valuation => valuation.Source == ValuationSource.Feed);

    public async Task<PagedResult<Investment>> GetPageAsync(
        Guid employeeId,
        InvestmentFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(
            dbContext.Investments.AsNoTracking().Where(investment => investment.EmployeeId == employeeId),
            filters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await Sort(query, filters)
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Investment>(items, filters.PageNumber, filters.PageSize, totalCount);
    }

    private static IQueryable<Investment> Filter(IQueryable<Investment> query, InvestmentFilters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var pattern = $"%{filters.Search.Trim()}%";
            query = query.Where(investment => EF.Functions.ILike(investment.AssetName, pattern));
        }

        if (filters.Types is { Count: > 0 })
        {
            var types = filters.Types.Distinct().ToArray();
            query = query.Where(investment => types.Contains(investment.Type));
        }

        if (filters.Currencies is { Count: > 0 })
        {
            var currencies = filters.Currencies.Distinct().ToArray();
            query = query.Where(investment => currencies.Contains(investment.InvestedAmount.Currency));
        }

        if (filters.PurchasedFrom is { } purchasedFrom)
        {
            query = query.Where(investment => investment.PurchasedOn >= purchasedFrom);
        }

        if (filters.PurchasedTo is { } purchasedTo)
        {
            query = query.Where(investment => investment.PurchasedOn <= purchasedTo);
        }

        if (filters.MinInvested is { } minInvested)
        {
            query = query.Where(investment => investment.InvestedAmount.Amount >= minInvested);
        }

        if (filters.MaxInvested is { } maxInvested)
        {
            query = query.Where(investment => investment.InvestedAmount.Amount <= maxInvested);
        }

        if (filters.Quoted is { } quoted)
        {
            query = query.Where(quoted ? IsQuoted : IsUnquoted);
        }

        return filters.Performance switch
        {
            InvestmentPerformance.Gain => query.Where(IsGaining),
            InvestmentPerformance.Loss => query.Where(IsLosing),
            _ => query
        };
    }

    private static IQueryable<Investment> Sort(IQueryable<Investment> query, InvestmentFilters filters)
    {
        var ascending = filters.SortDirection == SortDirection.Ascending;

        var ordered = filters.SortBy switch
        {
            InvestmentSortField.AssetName => ascending
                ? query.OrderBy(investment => investment.AssetName)
                : query.OrderByDescending(investment => investment.AssetName),
            InvestmentSortField.InvestedAmount => ascending
                ? query.OrderBy(investment => investment.InvestedAmount.Amount)
                : query.OrderByDescending(investment => investment.InvestedAmount.Amount),
            InvestmentSortField.CurrentValue => ascending
                ? query.OrderBy(LatestValue)
                : query.OrderByDescending(LatestValue),
            InvestmentSortField.ReturnPercentage => ascending
                ? query.OrderBy(ReturnRatio)
                : query.OrderByDescending(ReturnRatio),
            _ => ascending
                ? query.OrderBy(investment => investment.PurchasedOn)
                : query.OrderByDescending(investment => investment.PurchasedOn),
        };

        return ascending
            ? ordered.ThenBy(investment => investment.CreatedAt).ThenBy(investment => investment.Id)
            : ordered.ThenByDescending(investment => investment.CreatedAt).ThenByDescending(investment => investment.Id);
    }
}
