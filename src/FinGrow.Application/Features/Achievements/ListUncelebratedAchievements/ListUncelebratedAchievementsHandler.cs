namespace FinGrow.Application.Features.Achievements.ListUncelebratedAchievements;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>
/// Lo que el front tiene que celebrar: lo pide al abrir la app y cada vez que llega una
/// notificacion de logro.
/// </summary>
internal sealed class ListUncelebratedAchievementsHandler(IAchievementRepository achievements)
    : IRequestHandler<ListUncelebratedAchievementsQuery, Result<IReadOnlyList<AchievementResponse>>>
{
    public async Task<Result<IReadOnlyList<AchievementResponse>>> Handle(
        ListUncelebratedAchievementsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AchievementResponse> response = (await achievements.ListUncelebratedAsync(request.EmployeeId, cancellationToken))
            .OrderBy(earned => earned.AchievedAt)
            .Select(earned => AchievementResponse.From(AchievementCatalog.Find(earned.Code), earned))
            .ToList();

        return Result.Success(response);
    }
}
