namespace FinGrow.Api.UnitTests.Persistence;

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using FinGrow.Infrastructure.Persistence.Protection;

public class TokenEncryptionTests
{
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(TokenEncryptionOptions.KeyBytes);

    [Fact]
    public void A_protected_token_round_trips_and_never_appears_in_clear()
    {
        var protector = new AesGcmSecretProtector(_key);

        var ciphertext = protector.Protect("APP_USR-1234567890-token");

        ciphertext.ShouldNotContain("APP_USR");
        protector.Unprotect(ciphertext).ShouldBe("APP_USR-1234567890-token");
    }

    [Fact]
    public void Protecting_the_same_token_twice_yields_different_ciphertexts()
    {
        var protector = new AesGcmSecretProtector(_key);

        protector.Protect("token").ShouldNotBe(protector.Protect("token"));
    }

    [Fact]
    public void A_token_protected_with_another_key_cannot_be_read()
    {
        var ciphertext = new AesGcmSecretProtector(_key).Protect("token");
        var other = new AesGcmSecretProtector(RandomNumberGenerator.GetBytes(TokenEncryptionOptions.KeyBytes));

        Should.Throw<CryptographicException>(() => other.Unprotect(ciphertext));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("c2hvcnQ=")]
    public void The_key_must_be_thirty_two_random_bytes_in_base64(string key)
    {
        var options = new TokenEncryptionOptions { Key = key };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true).ShouldBeFalse();
    }
}
