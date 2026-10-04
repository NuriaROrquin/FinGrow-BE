namespace FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;

using Common;
using Domain.Enums;
using DTOs;
using MediatR;

public sealed record GetSecurityPriceQuery(string Symbol, Currency Currency) : IRequest<Result<SecurityPriceResponse>>;
