namespace FinGrow.Application.Features.Integrations.GetIntegration;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record GetIntegrationQuery(IntegrationProvider Provider) : IRequest<Result<IntegrationResponse>>;

public sealed record IntegrationResponse(bool Linked, string? ExternalAccountId, DateTimeOffset? LinkedAt)
{
    public static readonly IntegrationResponse NotLinked = new(false, null, null);
}
