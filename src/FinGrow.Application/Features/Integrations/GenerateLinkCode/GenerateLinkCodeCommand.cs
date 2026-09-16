namespace FinGrow.Application.Features.Integrations.GenerateLinkCode;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record GenerateLinkCodeCommand(IntegrationProvider Provider) : IRequest<Result<LinkCodeResponse>>;

public sealed record LinkCodeResponse(string Code, DateTimeOffset ExpiresAt);
