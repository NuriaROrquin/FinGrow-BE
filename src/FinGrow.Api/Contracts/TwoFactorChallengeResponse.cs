namespace FinGrow.Api.Contracts;

public sealed record TwoFactorChallengeResponse(bool RequiresTwoFactor, string ChallengeToken, DateTimeOffset ExpiresAt);
