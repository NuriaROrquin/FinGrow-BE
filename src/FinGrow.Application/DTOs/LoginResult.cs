namespace FinGrow.Application.DTOs;

public sealed record LoginResult(LoginResponse? Session, AuthToken? TwoFactorChallenge)
{
    public static LoginResult WithSession(LoginResponse session) => new(session, null);

    public static LoginResult RequiresTwoFactor(AuthToken challenge) => new(null, challenge);
}
