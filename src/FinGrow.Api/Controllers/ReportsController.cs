namespace FinGrow.Api.Controllers;

using Extensions;
using Application.Features.Reports.SavingsVsGoals;
using Application.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("savings-vs-goals")]
    public async Task<IActionResult> SavingsVsGoals(
        [FromQuery] Currency currency = Currency.ARS,
        [FromQuery] int? months = null,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetSavingsVsGoalsQuery(currentUser.UserId!.Value, currency, months),
            cancellationToken)).ToActionResult();
}
