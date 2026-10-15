namespace FinGrow.Application.Features.Integrations.Gmail.CompleteGmailLink;

using FinGrow.Application.Common;
using MediatR;

public sealed record CompleteGmailLinkCommand(string? Code, string? State, string? Error) : IRequest<Result>;
