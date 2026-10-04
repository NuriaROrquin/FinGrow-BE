namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.Dashboard.GetDashboard;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] Currency currency = Currency.ARS,
        [FromQuery(Name = "dateFrom")] DateOnly? fromDate = null,
        [FromQuery(Name = "dateTo")] DateOnly? toDate = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetDashboardQuery(currency, fromDate, toDate),
            cancellationToken)).ToActionResult();
}
