namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Integrations.WhatsApp.GenerateWhatsAppLinkCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/integrations")]
[Authorize]
public sealed class IntegrationsController : ControllerBase
{
    private readonly ISender _sender;

    public IntegrationsController(ISender sender) => _sender = sender;

    [HttpPost("whatsapp/link-code")]
    [ProducesResponseType<WhatsAppLinkCodeResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateWhatsAppLinkCode(CancellationToken cancellationToken) =>
        (await _sender.Send(new GenerateWhatsAppLinkCodeCommand(), cancellationToken)).ToActionResult();
}
