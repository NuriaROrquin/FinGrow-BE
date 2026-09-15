namespace FinGrow.Application.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid? CompanyId { get; }

    string? FullName { get; }

    string? Role { get; }

    DateTimeOffset? ExpiresAt { get; }

    bool IsAuthenticated { get; }
}
