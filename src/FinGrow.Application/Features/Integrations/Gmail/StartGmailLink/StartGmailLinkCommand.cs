namespace FinGrow.Application.Features.Integrations.Gmail.StartGmailLink;

using FinGrow.Application.Common;
using MediatR;

public sealed record StartGmailLinkCommand : IRequest<Result<StartGmailLinkResponse>>;

public sealed record StartGmailLinkResponse(Uri AuthorizationUrl, DateTimeOffset ExpiresAt);
