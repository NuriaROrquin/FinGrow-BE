namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.ExchangeRates.GetMepQuote;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/exchange-rates")]
[Authorize]
public sealed class ExchangeRatesController(ISender sender) : ControllerBase
{
    [HttpGet("mep")]
    public async Task<IActionResult> GetMep(CancellationToken cancellationToken) =>
        (await sender.Send(new GetMepQuoteQuery(), cancellationToken)).ToActionResult();
}
