namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Api.Telegram;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhooks/telegram")]
[AllowAnonymous]
[ValidateTelegramSecret]
public sealed class TelegramWebhookController : ControllerBase
{
    private readonly ISender _sender;

    public TelegramWebhookController(ISender sender) => _sender = sender;

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Receive([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        if (TelegramInboundMessage.ToCommand(update) is not { } command)
        {
            return Ok();
        }

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToActionResult();
    }
}
