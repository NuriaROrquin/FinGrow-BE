namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.SecurityPrices.GetSecurityPrice;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/security-prices")]
[Authorize]
public sealed class SecurityPricesController(ISender sender) : ControllerBase
{
    [HttpGet("{symbol}")]
    public async Task<IActionResult> Get(string symbol, [FromQuery] Currency currency, CancellationToken cancellationToken) =>
        (await sender.Send(new GetSecurityPriceQuery(symbol, currency), cancellationToken)).ToActionResult();
}
