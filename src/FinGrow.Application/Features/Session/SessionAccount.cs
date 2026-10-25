namespace FinGrow.Application.Features.Session;

internal sealed record SessionAccount(Guid UserId, Guid CompanyId, string Role, string FullName);
