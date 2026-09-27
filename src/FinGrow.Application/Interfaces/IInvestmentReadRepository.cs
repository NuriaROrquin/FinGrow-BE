namespace FinGrow.Application.Interfaces;

using Common;
using Domain.Entities;
using Features.Investments.ListInvestments;

public interface IInvestmentReadRepository
{
    Task<PagedResult<Investment>> GetPageAsync(
        Guid employeeId,
        InvestmentFilters filters,
        CancellationToken cancellationToken = default);
}
