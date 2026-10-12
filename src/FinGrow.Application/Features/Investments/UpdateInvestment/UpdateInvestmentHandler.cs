namespace FinGrow.Application.Features.Investments.UpdateInvestment;

using Common;
using DTOs;
using Interfaces;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class UpdateInvestmentHandler(
    IInvestmentRepository investmentRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateInvestmentCommand, Result<InvestmentResponse>>
{
    public async Task<Result<InvestmentResponse>> Handle(UpdateInvestmentCommand request, CancellationToken cancellationToken)
    {
        var owned = Ownership.Require(
            await investmentRepository.GetByIdAsync(request.Id, cancellationToken),
            investment => investment.EmployeeId,
            request.EmployeeId,
            InvestmentErrors.NotFound(request.Id),
            InvestmentErrors.NotOwned());

        if (owned.IsFailure)
        {
            return Result.Failure<InvestmentResponse>(owned.Error);
        }

        var investment = owned.Value;

        investment.Correct(
            request.AssetName,
            request.Type,
            Money.From(request.InvestedAmount, request.Currency),
            request.PurchasedOn,
            dateTimeProvider.UtcNow);

        investment.Track(request.Symbol, request.Quantity, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InvestmentResponse.FromEntity(investment));
    }
}
