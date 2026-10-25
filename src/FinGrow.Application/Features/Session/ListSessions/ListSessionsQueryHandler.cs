namespace FinGrow.Application.Features.Session.ListSessions;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListSessionsQueryHandler : IRequestHandler<ListSessionsQuery, Result<IReadOnlyList<ActiveSessionResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;

    public ListSessionsQueryHandler(ICurrentUser currentUser, IRefreshTokenRepository refreshTokens, IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<ActiveSessionResponse>>> Handle(ListSessionsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<IReadOnlyList<ActiveSessionResponse>>(SessionErrors.Unauthenticated);
        }

        var now = _clock.UtcNow;
        var current = await CurrentSession.FindAsync(_refreshTokens, request.CurrentRefreshToken, userId, now, cancellationToken);
        var tokens = await _refreshTokens.ListActiveForUserAsync(userId, now, cancellationToken);

        IReadOnlyList<ActiveSessionResponse> sessions = tokens
            .Select(token => new ActiveSessionResponse(
                token.SessionId,
                UserAgentDescription.BrowserOf(token.UserAgent),
                UserAgentDescription.OperatingSystemOf(token.UserAgent),
                token.StartedAt,
                token.CreatedAt,
                token.SessionId == current?.SessionId))
            .OrderByDescending(session => session.IsCurrent)
            .ThenByDescending(session => session.LastActiveAt)
            .ToList();

        return Result.Success(sessions);
    }
}
