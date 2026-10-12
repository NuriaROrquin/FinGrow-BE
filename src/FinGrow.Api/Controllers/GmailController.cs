namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Api.Gmail;
using FinGrow.Application.Features.Integrations.Gmail.CompleteGmailLink;
using FinGrow.Application.Features.Integrations.Gmail.StartGmailLink;
using FinGrow.Application.Features.Integrations.Gmail.Sync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/integrations/gmail")]
public sealed class GmailController : ControllerBase
{
    internal const string ResultQueryKey = "gmail";
    internal const string LinkedResult = "linked";
    internal const string ErrorResult = "error";

    private readonly ISender _sender;
    private readonly GmailReturnOptions _options;

    public GmailController(ISender sender, IOptions<GmailReturnOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost("oauth/start")]
    [Authorize]
    [ProducesResponseType<StartGmailLinkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Start(CancellationToken cancellationToken) =>
        (await _sender.Send(new StartGmailLinkCommand(), cancellationToken)).ToActionResult();

    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CompleteGmailLinkCommand(code, state, error), cancellationToken);

        var query = result.IsSuccess
            ? new Dictionary<string, string?> { [ResultQueryKey] = LinkedResult }
            : new Dictionary<string, string?> { [ResultQueryKey] = ErrorResult, ["reason"] = result.Error.Code };

        return Redirect(QueryHelpers.AddQueryString(_options.FrontendReturnUrl, query));
    }

    [HttpPost("sync")]
    [Authorize]
    [ProducesResponseType<GmailSyncSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken) =>
        (await _sender.Send(new SyncMyGmailCommand(), cancellationToken)).ToActionResult();
}
