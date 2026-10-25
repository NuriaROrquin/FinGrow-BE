namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Errors;

public class PasswordResetTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

    private static PasswordResetToken Issue(string value = "valor") =>
        PasswordResetToken.Issue(Guid.NewGuid(), value, Now);

    [Fact]
    public void Only_the_hash_of_the_value_is_stored()
    {
        var token = Issue("valor-secreto");

        token.TokenHash.ShouldBe(PasswordResetToken.Hash("valor-secreto"));
        token.TokenHash.ShouldNotContain("valor-secreto");
        token.TokenHash.Length.ShouldBe(PasswordResetToken.HashLength);
    }

    [Fact]
    public void It_is_usable_until_it_expires()
    {
        var token = Issue();

        token.IsUsable(Now + PasswordResetToken.Lifetime - TimeSpan.FromSeconds(1)).ShouldBeTrue();
        token.IsUsable(Now + PasswordResetToken.Lifetime).ShouldBeFalse();
    }

    [Fact]
    public void It_can_be_redeemed_only_once()
    {
        var token = Issue();

        token.Redeem(Now);

        token.UsedAt.ShouldBe(Now);
        token.IsUsable(Now).ShouldBeFalse();
        Should.Throw<DomainException>(() => token.Redeem(Now));
    }

    [Fact]
    public void An_expired_token_cannot_be_redeemed()
    {
        var token = Issue();

        Should.Throw<DomainException>(() => token.Redeem(Now + PasswordResetToken.Lifetime));
    }

    [Fact]
    public void Generated_values_are_url_safe_and_distinct()
    {
        var first = PasswordResetToken.GenerateValue();
        var second = PasswordResetToken.GenerateValue();

        first.ShouldNotBe(second);
        first.ShouldMatch("^[A-Za-z0-9_-]{43}$");
    }

    [Fact]
    public void It_always_belongs_to_a_user()
    {
        Should.Throw<DomainException>(() => PasswordResetToken.Issue(Guid.Empty, "valor", Now));
    }
}
