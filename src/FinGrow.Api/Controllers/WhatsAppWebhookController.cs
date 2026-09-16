namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Api.Twilio;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
[ValidateTwilioSignature]
public sealed class WhatsAppWebhookController(ISender sender) : ControllerBase
{

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces(TwiMlResult.MediaType)]
    public async Task<IActionResult> Receive([FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        var result = await sender.Send(TwilioInboundMessage.ToCommand(form), cancellationToken);

        return result.IsSuccess ? new TwiMlResult(result.Value.Text) : result.ToActionResult();
    }
}
