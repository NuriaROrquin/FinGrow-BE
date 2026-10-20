namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;

public class RefreshTokenTests
{
    private const string Chrome =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0 Safari/537.36";

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static RefreshToken Issue(string? userAgent = Chrome) =>
        RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "Empleado", RefreshToken.GenerateValue(), Now, userAgent);

    [Fact]
    public void A_new_token_starts_its_own_session()
    {
        var token = Issue();

        token.SessionId.ShouldBe(token.Id);
        token.StartedAt.ShouldBe(Now);
        token.UserAgent.ShouldBe(Chrome);
    }

    [Fact]
    public void Rotating_revokes_the_token_and_keeps_the_session_in_the_next_one()
    {
        var token = Issue();
        var later = Now.AddHours(2);

        var next = token.Rotate(RefreshToken.GenerateValue(), later, userAgent: null);

        token.RevokedAt.ShouldBe(later);
        next.Id.ShouldNotBe(token.Id);
        next.SessionId.ShouldBe(token.SessionId);
        next.StartedAt.ShouldBe(Now);
        next.CreatedAt.ShouldBe(later);
        next.ExpiresAt.ShouldBe(later.Add(RefreshToken.Lifetime));
        next.UserAgent.ShouldBe(Chrome);
        next.IsActive(later).ShouldBeTrue();
    }

    [Fact]
    public void Rotating_with_a_new_user_agent_replaces_it()
    {
        var next = Issue().Rotate(RefreshToken.GenerateValue(), Now.AddMinutes(20), "Mozilla/5.0 Firefox/140.0");

        next.UserAgent.ShouldBe("Mozilla/5.0 Firefox/140.0");
    }

    [Fact]
    public void A_user_agent_longer_than_the_column_is_truncated()
    {
        var token = Issue(new string('a', RefreshToken.MaxUserAgentLength + 50));

        token.UserAgent!.Length.ShouldBe(RefreshToken.MaxUserAgentLength);
    }

    [Fact]
    public void A_blank_user_agent_is_stored_as_unknown()
    {
        Issue("   ").UserAgent.ShouldBeNull();
    }
}
