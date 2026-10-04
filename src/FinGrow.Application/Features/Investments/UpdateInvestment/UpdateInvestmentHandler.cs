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
        var investment = await investmentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (investment is null || investment.EmployeeId != request.EmployeeId)
        {
            return Result.Failure<InvestmentResponse>(
                Error.NotFound("Investment.NotFound", $"No existe una inversion con id '{request.Id}'."));
        }

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
