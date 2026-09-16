namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Integrations.WhatsApp.GenerateWhatsAppLinkCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/integrations")]
[Authorize]
public sealed class IntegrationsController(ISender sender) : ControllerBase
{
    [HttpPost("whatsapp/link-code")]
    [ProducesResponseType<WhatsAppLinkCodeResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateWhatsAppLinkCode(CancellationToken cancellationToken) =>
        (await sender.Send(new GenerateWhatsAppLinkCodeCommand(), cancellationToken)).ToActionResult();
}
