namespace FinGrow.Application.Features.Investments.ListInvestments;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class ListInvestmentsHandler(IInvestmentRepository investmentRepository)
    : IRequestHandler<ListInvestmentsQuery, Result<IReadOnlyList<InvestmentResponse>>>
{
    public async Task<Result<IReadOnlyList<InvestmentResponse>>> Handle(ListInvestmentsQuery request, CancellationToken cancellationToken)
    {
        var investments = await investmentRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        return Result.Success<IReadOnlyList<InvestmentResponse>>(
            investments.Select(InvestmentResponse.FromEntity).ToList());
    }
}
