namespace FinGrow.Application.Features.Investments.DeleteInvestment;

using Common;
using Interfaces;
using Domain.Repositories;
using MediatR;

internal sealed class DeleteInvestmentHandler(
    IInvestmentRepository investmentRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteInvestmentCommand, Result>
{
    public async Task<Result> Handle(DeleteInvestmentCommand request, CancellationToken cancellationToken)
    {
        var investment = await investmentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (investment is null || investment.EmployeeId != request.EmployeeId)
        {
            return Result.Failure(
                Error.NotFound("Investment.NotFound", $"No existe una inversion con id '{request.Id}'."));
        }

        investmentRepository.Remove(investment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
