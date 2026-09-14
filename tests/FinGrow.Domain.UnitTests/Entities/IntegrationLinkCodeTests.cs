namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class IntegrationLinkCodeTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_generated_code_has_the_expected_length_and_no_ambiguous_characters()
    {
        var code = IntegrationLinkCode.GenerateCode();

        code.Length.ShouldBe(IntegrationLinkCode.Length);
        code.ShouldNotContain('0');
        code.ShouldNotContain('O');
        code.ShouldNotContain('1');
        code.ShouldNotContain('I');
    }

    [Fact]
    public void The_code_is_stored_hashed_and_expires_after_its_lifetime()
    {
        var linkCode = IntegrationLinkCode.Create(EmployeeId, IntegrationProvider.WhatsApp, "ABCD2345", Now);

        linkCode.CodeHash.ShouldNotContain("ABCD2345");
        linkCode.CodeHash.ShouldBe(IntegrationLinkCode.Hash("ABCD2345"));
        linkCode.ExpiresAt.ShouldBe(Now.Add(IntegrationLinkCode.Lifetime));
        linkCode.UsedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("abcd2345")]
    [InlineData(" ABCD 2345 ")]
    [InlineData("ABCD2345")]
    public void Hashing_ignores_case_and_spaces_because_the_code_is_typed_on_a_phone(string typed)
    {
        IntegrationLinkCode.Hash(typed).ShouldBe(IntegrationLinkCode.Hash("ABCD2345"));
    }

    [Fact]
    public void A_code_with_the_wrong_length_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            IntegrationLinkCode.Create(EmployeeId, IntegrationProvider.WhatsApp, "ABC", Now));
    }

    [Fact]
    public void A_code_always_belongs_to_an_employee()
    {
        Should.Throw<DomainException>(() =>
            IntegrationLinkCode.Create(Guid.Empty, IntegrationProvider.WhatsApp, "ABCD2345", Now));
    }

    [Fact]
    public void Redeeming_marks_the_code_as_used_and_it_cannot_be_redeemed_twice()
    {
        var linkCode = IntegrationLinkCode.Create(EmployeeId, IntegrationProvider.WhatsApp, "ABCD2345", Now);

        linkCode.Redeem(Now.AddMinutes(1));

        linkCode.UsedAt.ShouldBe(Now.AddMinutes(1));
        linkCode.IsUsable(Now.AddMinutes(2)).ShouldBeFalse();
        Should.Throw<DomainException>(() => linkCode.Redeem(Now.AddMinutes(2)));
    }

    [Fact]
    public void An_expired_code_cannot_be_redeemed()
    {
        var linkCode = IntegrationLinkCode.Create(EmployeeId, IntegrationProvider.WhatsApp, "ABCD2345", Now);
        var afterExpiry = Now.Add(IntegrationLinkCode.Lifetime);

        linkCode.IsUsable(afterExpiry).ShouldBeFalse();
        Should.Throw<DomainException>(() => linkCode.Redeem(afterExpiry));
    }
}
