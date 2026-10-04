namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.SecurityPrices.GetSecurityPrice;
using Application.Features.SecurityPrices.SearchSecurityPrices;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/security-prices")]
[Authorize]
public sealed class SecurityPricesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string symbol,
        [FromQuery] Currency currency,
        [FromQuery] InvestmentType type,
        CancellationToken cancellationToken) =>
        (await sender.Send(new GetSecurityPriceQuery(symbol, currency, type), cancellationToken)).ToActionResult();

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] InvestmentType type,
        [FromQuery] string query,
        [FromQuery] Currency? currency,
        CancellationToken cancellationToken) =>
        (await sender.Send(new SearchSecurityPricesQuery(type, query, currency), cancellationToken)).ToActionResult();
}
