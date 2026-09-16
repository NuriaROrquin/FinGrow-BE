namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("login")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("empleado")]
    public async Task<IActionResult> LoginEmpleado(LoginEmployeeCommand command, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken)).ToActionResult();
}
