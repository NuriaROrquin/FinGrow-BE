namespace FinGrow.Application.Features.Session.CloseOtherSessions;

using FinGrow.Application.Common;
using MediatR;

public sealed record CloseOtherSessionsCommand : IRequest<Result>;
