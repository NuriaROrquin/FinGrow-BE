namespace FinGrow.Application.Features.ExchangeRates.GetMepQuote;

using Common;
using DTOs;
using MediatR;

public sealed record GetMepQuoteQuery : IRequest<Result<MepQuoteResponse>>;
