namespace FinGrow.Api.Contracts;

public sealed record TwoFactorLoginRequest(string ChallengeToken, string Code);
