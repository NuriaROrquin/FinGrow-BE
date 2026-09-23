namespace FinGrow.Application.Features.Session;

using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;

internal sealed class SessionIssuer
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;

    public SessionIssuer(ITokenService tokenService, IRefreshTokenRepository refreshTokens, IDateTimeProvider clock)
    {
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public LoginResponse Issue(Guid userId, Guid companyId, string role, string fullName)
    {
        var now = _clock.UtcNow;
        var accessToken = _tokenService.GenerateToken(userId, companyId, role, fullName);

        var refreshValue = RefreshToken.GenerateValue();
        var refreshToken = RefreshToken.Issue(userId, companyId, role, refreshValue, now);
        _refreshTokens.Add(refreshToken);

        return new LoginResponse(
            userId,
            companyId,
            fullName,
            role,
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshValue,
            refreshToken.ExpiresAt);
    }
}
