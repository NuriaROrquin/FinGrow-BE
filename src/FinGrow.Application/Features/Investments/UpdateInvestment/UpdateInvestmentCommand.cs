namespace FinGrow.Application.Features.Investments.UpdateInvestment;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record UpdateInvestmentCommand(
    Guid Id,
    Guid EmployeeId,
    string AssetName,
    InvestmentType Type,
    decimal InvestedAmount,
    Currency Currency,
    DateOnly PurchasedOn) : IRequest<Result<InvestmentResponse>>;
