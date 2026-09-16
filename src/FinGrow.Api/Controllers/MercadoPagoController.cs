namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Api.MercadoPago;
using FinGrow.Application.Features.Integrations.MercadoPago.CompleteMercadoPagoLink;
using FinGrow.Application.Features.Integrations.MercadoPago.StartMercadoPagoLink;
using FinGrow.Application.Features.Integrations.MercadoPago.Sync;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/integrations/mercadopago")]
public sealed class MercadoPagoController : ControllerBase
{
    internal const string ResultQueryKey = "mercadopago";
    internal const string LinkedResult = "linked";
    internal const string ErrorResult = "error";

    private readonly ISender _sender;
    private readonly MercadoPagoReturnOptions _options;

    public MercadoPagoController(ISender sender, IOptions<MercadoPagoReturnOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpPost("oauth/start")]
    [Authorize]
    [ProducesResponseType<StartMercadoPagoLinkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Start(CancellationToken cancellationToken) =>
        (await _sender.Send(new StartMercadoPagoLinkCommand(), cancellationToken)).ToActionResult();

    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CompleteMercadoPagoLinkCommand(code, state, error), cancellationToken);

        var query = result.IsSuccess
            ? new Dictionary<string, string?> { [ResultQueryKey] = LinkedResult }
            : new Dictionary<string, string?> { [ResultQueryKey] = ErrorResult, ["reason"] = result.Error.Code };

        return Redirect(QueryHelpers.AddQueryString(_options.FrontendReturnUrl, query));
    }

    [HttpPost("sync")]
    [Authorize]
    [ProducesResponseType<MercadoPagoSyncSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken) =>
        (await _sender.Send(new SyncMyMercadoPagoCommand(), cancellationToken)).ToActionResult();
}
