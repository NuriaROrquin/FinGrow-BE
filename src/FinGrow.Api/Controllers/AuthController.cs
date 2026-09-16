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
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("empleado")]
    public async Task<IActionResult> LoginEmpleado(LoginEmployeeCommand command, CancellationToken cancellationToken) =>
        StartSession(await _mediator.Send(command, cancellationToken));

    [HttpPost("empresa")]
    public async Task<IActionResult> LoginEmpresa(LoginCompanyCommand command, CancellationToken cancellationToken) =>
        StartSession(await _mediator.Send(command, cancellationToken));

    private IActionResult StartSession(Result<LoginResponse> result)
    {
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        var login = result.Value;
        SessionCookie.Append(Response, login.Token, login.ExpiresAt);

        return Ok(new SessionResponse(login.EmployeeId, login.CompanyId, login.FullName, login.Role, login.ExpiresAt));
    }
}
