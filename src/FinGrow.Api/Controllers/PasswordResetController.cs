namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.PasswordReset.RequestPasswordReset;
using FinGrow.Application.Features.PasswordReset.ResetPassword;
using FinGrow.Application.Features.PasswordReset.ValidateToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>HU-72. El rol va en la ruta, como en <see cref="AuthController"/>: cada login tiene su link.</summary>
[ApiController]
[Route("password-reset")]
public sealed class PasswordResetController(IMediator mediator) : ControllerBase
{
    [HttpPost("empleado")]
    public async Task<IActionResult> RequestEmpleado(PasswordResetRequest request, CancellationToken cancellationToken) =>
        (await mediator.Send(new RequestPasswordResetCommand(request.Email, Rol.Empleado), cancellationToken)).ToActionResult();

    [HttpPost("empresa")]
    public async Task<IActionResult> RequestEmpresa(PasswordResetRequest request, CancellationToken cancellationToken) =>
        (await mediator.Send(new RequestPasswordResetCommand(request.Email, Rol.Empresa), cancellationToken)).ToActionResult();

    // POST y no GET: el token no tiene que quedar en los logs de acceso.
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(ValidatePasswordResetTokenQuery query, CancellationToken cancellationToken) =>
        (await mediator.Send(query, cancellationToken)).ToActionResult();

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            SessionCookie.Delete(Response);
        }

        return result.ToActionResult();
    }
}
