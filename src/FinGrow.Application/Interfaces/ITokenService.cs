namespace FinGrow.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Guid userId, Guid companyId, string role);
}
