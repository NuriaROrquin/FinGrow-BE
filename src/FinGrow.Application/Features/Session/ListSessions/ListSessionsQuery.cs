namespace FinGrow.Application.Features.Session.ListSessions;

using FinGrow.Application.Common;
using MediatR;

public sealed record ListSessionsQuery : IRequest<Result<IReadOnlyList<ActiveSessionResponse>>>;
