namespace FinGrow.Infrastructure.Identity;

using System.Security.Claims;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Http;

internal sealed class CurrentUser : ICurrentUser
{
    public const string CompanyIdClaim = "company_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId => ReadGuidClaim(ClaimTypes.NameIdentifier);

    public Guid? CompanyId => ReadGuidClaim(CompanyIdClaim);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    private Guid? ReadGuidClaim(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
