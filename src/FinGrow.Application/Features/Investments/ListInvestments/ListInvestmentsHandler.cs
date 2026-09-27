namespace FinGrow.Application.Features.Investments.ListInvestments;

using Common;
using DTOs;
using Interfaces;
using MediatR;

internal sealed class ListInvestmentsHandler(IInvestmentReadRepository investmentReadRepository)
    : IRequestHandler<ListInvestmentsQuery, Result<PagedResult<InvestmentResponse>>>
{
    public async Task<Result<PagedResult<InvestmentResponse>>> Handle(ListInvestmentsQuery request, CancellationToken cancellationToken)
    {
        var page = await investmentReadRepository.GetPageAsync(request.EmployeeId, request.Filters, cancellationToken);

        return Result.Success(new PagedResult<InvestmentResponse>(
            page.Items.Select(InvestmentResponse.FromEntity).ToList(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount));
    }
}
