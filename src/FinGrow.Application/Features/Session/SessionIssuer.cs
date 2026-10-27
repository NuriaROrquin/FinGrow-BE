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
    private readonly IClientDevice _device;

    public SessionIssuer(
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IDateTimeProvider clock,
        IClientDevice device)
    {
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _device = device;
    }

    /// <summary>Abre una sesion nueva (login).</summary>
    public LoginResponse Issue(Guid userId, Guid companyId, string role, string fullName)
    {
        var refreshValue = RefreshToken.GenerateValue();
        var refreshToken = RefreshToken.Issue(userId, companyId, role, refreshValue, _clock.UtcNow, _device.UserAgent);

        return Complete(refreshToken, refreshValue, fullName);
    }

    /// <summary>Renueva una sesion abierta: revoca su token y emite el siguiente, que sigue siendo la
    /// misma sesion para la lista de sesiones abiertas (HU-73).</summary>
    public LoginResponse Renew(RefreshToken current, string fullName)
    {
        var refreshValue = RefreshToken.GenerateValue();
        var refreshToken = current.Rotate(refreshValue, _clock.UtcNow, _device.UserAgent);

        return Complete(refreshToken, refreshValue, fullName);
    }

    private LoginResponse Complete(RefreshToken refreshToken, string refreshValue, string fullName)
    {
        var userId = refreshToken.UserId;
        var companyId = refreshToken.CompanyId;
        var role = refreshToken.Role;
        var accessToken = _tokenService.GenerateToken(userId, companyId, role, fullName, refreshToken.SessionId);
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
