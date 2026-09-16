using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;

namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("login")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("empleado")]
    public async Task<IActionResult> LoginEmpleado(LoginEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        var login = result.Value;
        SessionCookie.Append(Response, login.Token, login.ExpiresAt);

        return Ok(new SessionResponse(login.EmployeeId, login.CompanyId, login.FullName, login.Role, login.ExpiresAt));
    }
}
