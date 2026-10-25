namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Features.PasswordReset.RequestPasswordReset;
using FinGrow.Application.Features.PasswordReset.ResetPassword;
using FinGrow.Application.Features.PasswordReset.ValidateToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("password-reset")]
public sealed class PasswordResetController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RequestLink(PasswordResetRequest request, CancellationToken cancellationToken) =>
        (await mediator.Send(new RequestPasswordResetCommand(request.Email), cancellationToken)).ToActionResult();

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
