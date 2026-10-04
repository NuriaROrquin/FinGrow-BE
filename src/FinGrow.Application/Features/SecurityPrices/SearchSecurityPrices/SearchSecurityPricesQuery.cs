namespace FinGrow.Application.Features.SecurityPrices.SearchSecurityPrices;

using Common;
using Domain.Enums;
using DTOs;
using MediatR;

public sealed record SearchSecurityPricesQuery(InvestmentType Type, string Query, Currency? Currency = null)
    : IRequest<Result<IReadOnlyList<SecurityPriceResponse>>>;
