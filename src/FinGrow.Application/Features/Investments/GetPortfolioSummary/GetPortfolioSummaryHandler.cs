namespace FinGrow.Application.Features.Investments.GetPortfolioSummary;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class GetPortfolioSummaryHandler(IInvestmentRepository investmentRepository)
    : IRequestHandler<GetPortfolioSummaryQuery, Result<PortfolioSummaryResponse>>
{
    public async Task<Result<PortfolioSummaryResponse>> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        var investments = await investmentRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        return Result.Success(PortfolioSummaryResponse.FromInvestments(investments));
    }
}
