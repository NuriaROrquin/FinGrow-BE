namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void The_email_is_stored_normalized_in_lowercase_and_without_spaces()
    {
        Email.From("  Juan.Perez@Empresa.COM ").Value.ShouldBe("juan.perez@empresa.com");
    }

    [Fact]
    public void Two_emails_that_differ_only_in_case_are_the_same_person()
    {
        Email.From("ana@fingrow.com").ShouldBe(Email.From("ANA@fingrow.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sinarroba.com")]
    [InlineData("@empresa.com")]
    [InlineData("juan@")]
    [InlineData("juan@empresa")]
    [InlineData("juan@@empresa.com")]
    [InlineData("juan perez@empresa.com")]
    public void A_malformed_email_is_rejected_when_constructed(string candidate)
    {
        Should.Throw<DomainException>(() => Email.From(candidate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sinarroba.com")]
    [InlineData("juan@empresa")]
    [InlineData("juan perez@empresa.com")]
    public void IsValid_rejects_the_same_emails_that_cannot_be_constructed(string candidate)
    {
        Email.IsValid(candidate).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_accepts_an_email_with_spaces_around_and_uppercase()
    {
        Email.IsValid("  Juan.Perez@Empresa.COM ").ShouldBeTrue();
    }
}
