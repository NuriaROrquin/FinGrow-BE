namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Login;
using FinGrow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("login")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("empleado")]
    public async Task<IActionResult> LoginEmpleado(LoginRequest request, CancellationToken cancellationToken) =>
        StartSessionOrChallenge(await mediator.Send(
            new LoginCommand(request.Email, request.Password, UserRole.Employee),
            cancellationToken));

    [HttpPost("empleado/2fa")]
    public async Task<IActionResult> VerificarDobleFactor(TwoFactorLoginRequest request, CancellationToken cancellationToken) =>
        StartSession(await mediator.Send(
            new VerifyTwoFactorLoginCommand(request.ChallengeToken, request.Code, UserRole.Employee),
            cancellationToken));

    [HttpPost("empresa")]
    public async Task<IActionResult> LoginEmpresa(LoginRequest request, CancellationToken cancellationToken) =>
        StartSessionOrChallenge(await mediator.Send(
            new LoginCommand(request.Email, request.Password, UserRole.Company),
            cancellationToken));

    [HttpPost("empresa/2fa")]
    public async Task<IActionResult> VerificarDobleFactorEmpresa(TwoFactorLoginRequest request, CancellationToken cancellationToken) =>
        StartSession(await mediator.Send(
            new VerifyTwoFactorLoginCommand(request.ChallengeToken, request.Code, UserRole.Company),
            cancellationToken));

    private IActionResult StartSessionOrChallenge(Result<LoginResult> result)
    {
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        if (result.Value.TwoFactorChallenge is { } challenge)
        {
            return Ok(new TwoFactorChallengeResponse(true, challenge.Value, challenge.ExpiresAt));
        }

        return StartSession(Result.Success(result.Value.Session!));
    }

    private IActionResult StartSession(Result<LoginResponse> result)
    {
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        var login = result.Value;
        SessionCookie.Append(Response, login.Token, login.ExpiresAt);
        SessionCookie.AppendRefresh(Response, login.RefreshToken, login.RefreshExpiresAt);

        return Ok(new SessionResponse(login.EmployeeId, login.CompanyId, login.FullName, login.Role, login.ExpiresAt));
    }
}
