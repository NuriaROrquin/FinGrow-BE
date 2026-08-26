namespace FinGrow.Application.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid? CompanyId { get; }

    bool IsAuthenticated { get; }
}
