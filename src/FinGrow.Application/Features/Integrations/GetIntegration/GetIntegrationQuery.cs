namespace FinGrow.Application.Features.Integrations.GetIntegration;

using FinGrow.Application.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record GetIntegrationQuery(IntegrationProvider Provider) : IRequest<Result<IntegrationResponse>>;

public sealed record IntegrationResponse(
    bool Linked,
    string? ExternalAccountId,
    DateTimeOffset? LinkedAt,
    IntegrationStatus? Status = null,
    string? ErrorReason = null)
{
    public static readonly IntegrationResponse NotLinked = new(false, null, null);

    public static IntegrationResponse From(EmployeeIntegration integration) =>
        new(true, integration.ExternalAccountId, integration.LinkedAt, integration.Status, integration.ErrorReason);
}
