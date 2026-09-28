namespace FinGrow.Application.Features.Investments.CreateInvestment;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record CreateInvestmentCommand(
    Guid EmployeeId,
    string AssetName,
    InvestmentType Type,
    decimal InvestedAmount,
    Currency Currency,
    DateOnly PurchasedOn) : IRequest<Result<InvestmentResponse>>;
