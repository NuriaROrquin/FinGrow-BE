namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.GenerateLinkCode;
using FinGrow.Application.Features.Integrations.GetIntegration;
using FinGrow.Application.Features.Integrations.UnlinkIntegration;
using FinGrow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/integrations/{provider}")]
[Authorize(Roles = Rol.Empleado)]
public sealed class IntegrationsController : ControllerBase
{
    private readonly ISender _sender;

    public IntegrationsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType<IntegrationResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(IntegrationProvider provider, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetIntegrationQuery(provider), cancellationToken)).ToActionResult();

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlink(IntegrationProvider provider, CancellationToken cancellationToken) =>
        (await _sender.Send(new UnlinkIntegrationCommand(provider), cancellationToken)).ToActionResult();

    [HttpPost("link-code")]
    [ProducesResponseType<LinkCodeResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateLinkCode(IntegrationProvider provider, CancellationToken cancellationToken) =>
        (await _sender.Send(new GenerateLinkCodeCommand(provider), cancellationToken)).ToActionResult();
}
