namespace FinGrow.Api.Controllers;

using FinGrow.Api.Authentication;
using FinGrow.Api.Contracts;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("session")]
public sealed class SessionController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public SessionController(ICurrentUser currentUser) => _currentUser = currentUser;

    [HttpGet]
    [Authorize]
    public ActionResult<SessionResponse> Get() =>
        new SessionResponse(
            _currentUser.UserId!.Value,
            _currentUser.CompanyId!.Value,
            _currentUser.FullName ?? string.Empty,
            _currentUser.Role ?? string.Empty,
            _currentUser.ExpiresAt ?? DateTimeOffset.MinValue);

    [HttpDelete]
    public IActionResult Delete()
    {
        SessionCookie.Delete(Response);
        return NoContent();
    }
}
