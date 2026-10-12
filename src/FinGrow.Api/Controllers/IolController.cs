namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.GetIntegration;
using FinGrow.Application.Features.Integrations.Iol.LinkIol;
using FinGrow.Application.Features.Integrations.Iol.Sync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/integrations/iol")]
[Authorize(Roles = Rol.Empleado)]
public sealed class IolController : ControllerBase
{
    private readonly ISender _sender;

    public IolController(ISender sender) => _sender = sender;

    [HttpPost("link")]
    [ProducesResponseType<IntegrationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Link(LinkIolCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult();

    [HttpPost("sync")]
    [ProducesResponseType<IolSyncSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken) =>
        (await _sender.Send(new SyncMyIolPortfolioCommand(), cancellationToken)).ToActionResult();
}
