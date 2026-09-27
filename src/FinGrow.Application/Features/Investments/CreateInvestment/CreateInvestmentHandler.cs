namespace FinGrow.Application.Features.Investments.CreateInvestment;

using Common;
using DTOs;
using Interfaces;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class CreateInvestmentHandler(
    IInvestmentRepository investmentRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateInvestmentCommand, Result<InvestmentResponse>>
{
    public async Task<Result<InvestmentResponse>> Handle(CreateInvestmentCommand request, CancellationToken cancellationToken)
    {
        var investment = Investment.Create(
            request.EmployeeId,
            request.AssetName,
            request.Type,
            Money.From(request.InvestedAmount, request.Currency),
            request.PurchasedOn,
            dateTimeProvider.UtcNow);

        investmentRepository.Add(investment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InvestmentResponse.FromEntity(investment));
    }
}
