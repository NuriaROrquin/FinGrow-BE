namespace FinGrow.Domain.Common;

using FinGrow.Domain.ValueObjects;

public interface ITwoFactorAccount
{
    Email Email { get; }

    string? TwoFactorSecret { get; }

    bool IsTwoFactorEnabled { get; }

    void StartTwoFactorEnrollment(string secret, DateTimeOffset updatedAt);

    void EnableTwoFactor(DateTimeOffset enabledAt);

    void DisableTwoFactor(DateTimeOffset updatedAt);
}
