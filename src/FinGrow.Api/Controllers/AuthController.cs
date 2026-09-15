namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
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
        (await _mediator.Send(command, cancellationToken)).ToActionResult();
}
