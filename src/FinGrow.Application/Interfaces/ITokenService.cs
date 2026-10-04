namespace FinGrow.Application.Interfaces;

using FinGrow.Application.DTOs;

public interface ITokenService
{
    AuthToken GenerateToken(Guid userId, Guid companyId, string role, string fullName);

    AuthToken GenerateTwoFactorChallenge(Guid employeeId);

    Guid? ReadTwoFactorChallenge(string challengeToken);
}
