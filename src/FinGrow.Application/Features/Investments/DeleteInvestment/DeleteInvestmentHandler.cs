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
        var owned = Ownership.Require(
            await investmentRepository.GetByIdAsync(request.Id, cancellationToken),
            investment => investment.EmployeeId,
            request.EmployeeId,
            InvestmentErrors.NotFound(request.Id),
            InvestmentErrors.NotOwned());

        if (owned.IsFailure)
        {
            return Result.Failure(owned.Error);
        }

        var investment = owned.Value;

        investmentRepository.Remove(investment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
