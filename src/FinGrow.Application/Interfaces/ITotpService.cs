namespace FinGrow.Application.Interfaces;

public interface ITotpService
{
    string GenerateSecret();

    bool VerifyCode(string secret, string code, DateTimeOffset now);

    string BuildProvisioningUri(string secret, string accountName);
}
