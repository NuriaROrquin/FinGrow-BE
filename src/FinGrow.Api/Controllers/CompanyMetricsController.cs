namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.GetParticipation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/company/metrics")]
[Authorize(Roles = Rol.Empresa)]
public sealed class CompanyMetricsController(ISender sender) : ControllerBase
{
    [HttpGet("participation")]
    public async Task<IActionResult> Participation(CancellationToken cancellationToken) =>
        (await sender.Send(new GetParticipationQuery(), cancellationToken)).ToActionResult();
}
