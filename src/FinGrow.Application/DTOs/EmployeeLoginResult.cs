namespace FinGrow.Application.DTOs;

public sealed record EmployeeLoginResult(LoginResponse? Session, AuthToken? TwoFactorChallenge)
{
    public static EmployeeLoginResult WithSession(LoginResponse session) => new(session, null);

    public static EmployeeLoginResult RequiresTwoFactor(AuthToken challenge) => new(null, challenge);
}
