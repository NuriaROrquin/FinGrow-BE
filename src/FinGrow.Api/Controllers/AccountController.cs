namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Account.ChangePassword;
using FinGrow.Application.Features.Account.TwoFactor;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("account")]
[Authorize]
public sealed class AccountController(IMediator mediator) : ControllerBase
{
    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            SessionCookie.Delete(Response);
        }

        return result.ToActionResult();
    }

    [HttpPost("2fa/setup")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken cancellationToken) =>
        (await mediator.Send(new SetupTwoFactorCommand(), cancellationToken)).ToActionResult();

    [HttpPost("2fa/enable")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorCommand command, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken)).ToActionResult();
}
