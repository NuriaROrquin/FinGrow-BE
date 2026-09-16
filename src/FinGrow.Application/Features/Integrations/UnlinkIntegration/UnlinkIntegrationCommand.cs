namespace FinGrow.Application.Features.Integrations.UnlinkIntegration;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record UnlinkIntegrationCommand(IntegrationProvider Provider) : IRequest<Result>;
