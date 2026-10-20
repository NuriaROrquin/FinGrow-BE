namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Features.Session.CloseOtherSessions;
using FinGrow.Application.Features.Session.CloseSession;
using FinGrow.Application.Features.Session.ListSessions;
using FinGrow.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("session")]
public sealed class SessionController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public SessionController(ICurrentUser currentUser, IMediator mediator)
    {
        _currentUser = currentUser;
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize]
    public ActionResult<SessionResponse> Get() =>
        new SessionResponse(
            _currentUser.UserId!.Value,
            _currentUser.CompanyId!.Value,
            _currentUser.FullName ?? string.Empty,
            _currentUser.Role ?? string.Empty,
            _currentUser.ExpiresAt ?? DateTimeOffset.MinValue);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(SessionCookie.RefreshName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            SessionCookie.Delete(Response);
            return Unauthorized();
        }

        var result = await _mediator.Send(new RefreshSessionCommand(refreshToken), cancellationToken);

        if (result.IsFailure)
        {
            SessionCookie.Delete(Response);
            return result.ToActionResult();
        }

        var session = result.Value;
        SessionCookie.Append(Response, session.Token, session.ExpiresAt);
        SessionCookie.AppendRefresh(Response, session.RefreshToken, session.RefreshExpiresAt);

        return Ok(new SessionResponse(session.EmployeeId, session.CompanyId, session.FullName, session.Role, session.ExpiresAt));
    }

    // La sesion actual se reconoce por la cookie de refresh, que el navegador solo manda a /session:
    // por eso estas rutas cuelgan de aca y no de /api.
    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await _mediator.Send(new ListSessionsQuery(CurrentRefreshToken()), cancellationToken)).ToActionResult();

    [HttpDelete("{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> Close(Guid sessionId, CancellationToken cancellationToken) =>
        (await _mediator.Send(new CloseSessionCommand(sessionId, CurrentRefreshToken()), cancellationToken)).ToActionResult();

    [HttpDelete("others")]
    [Authorize]
    public async Task<IActionResult> CloseOthers(CancellationToken cancellationToken) =>
        (await _mediator.Send(new CloseOtherSessionsCommand(CurrentRefreshToken()), cancellationToken)).ToActionResult();

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(SessionCookie.RefreshName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            await _mediator.Send(new RevokeSessionCommand(refreshToken), cancellationToken);
        }

        SessionCookie.Delete(Response);
        return NoContent();
    }

    private string? CurrentRefreshToken() =>
        Request.Cookies.TryGetValue(SessionCookie.RefreshName, out var value) && !string.IsNullOrEmpty(value) ? value : null;
}
