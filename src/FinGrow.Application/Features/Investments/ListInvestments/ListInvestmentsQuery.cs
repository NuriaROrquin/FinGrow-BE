namespace FinGrow.Application.Features.Investments.ListInvestments;

using Common;
using DTOs;
using MediatR;

public sealed record ListInvestmentsQuery(Guid EmployeeId, InvestmentFilters Filters)
    : IRequest<Result<PagedResult<InvestmentResponse>>>;
