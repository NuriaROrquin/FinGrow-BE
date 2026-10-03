namespace FinGrow.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

internal sealed class JwtTokenService : ITokenService
{
    private const string TwoFactorAudienceSuffix = ".2fa";

    private static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    private string TwoFactorAudience => _options.Audience + TwoFactorAudienceSuffix;

    private SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(_options.SecretKey));

    public AuthToken GenerateToken(Guid userId, Guid companyId, string role, string fullName)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(CurrentUser.CompanyIdClaim, companyId.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, fullName),
        ];

        return WriteToken(claims, _options.Audience, DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes));
    }

    public AuthToken GenerateTwoFactorChallenge(Guid employeeId) =>
        WriteToken(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, employeeId.ToString()) },
            TwoFactorAudience,
            DateTime.UtcNow.Add(TwoFactorChallengeLifetime));

    public Guid? ReadTwoFactorChallenge(string challengeToken)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = TwoFactorAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            var principal = handler.ValidateToken(challengeToken, parameters, out _);
            return Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var employeeId)
                ? employeeId
                : null;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private AuthToken WriteToken(IEnumerable<Claim> claims, string audience, DateTime expiresAt)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
