namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("login")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("empleado")]
    public async Task<IActionResult> LoginEmpleado(LoginEmployeeCommand command, CancellationToken cancellationToken) =>
        StartSession(await mediator.Send(command, cancellationToken));

    [HttpPost("empresa")]
    public async Task<IActionResult> LoginEmpresa(LoginCompanyCommand command, CancellationToken cancellationToken) =>
        StartSession(await mediator.Send(command, cancellationToken));

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
