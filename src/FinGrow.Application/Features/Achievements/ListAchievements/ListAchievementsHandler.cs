namespace FinGrow.Application.Features.Achievements.ListAchievements;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>El catalogo completo, con la fecha de los que el empleado ya gano.</summary>
internal sealed class ListAchievementsHandler(IAchievementRepository achievements)
    : IRequestHandler<ListAchievementsQuery, Result<IReadOnlyList<AchievementResponse>>>
{
    public async Task<Result<IReadOnlyList<AchievementResponse>>> Handle(
        ListAchievementsQuery request,
        CancellationToken cancellationToken)
    {
        var earned = (await achievements.ListByEmployeeAsync(request.EmployeeId, cancellationToken))
            .ToDictionary(achievement => achievement.Code);

        IReadOnlyList<AchievementResponse> response = AchievementCatalog.All
            .Select(achievement => AchievementResponse.From(achievement, earned.GetValueOrDefault(achievement.Code)))
            .ToList();

        return Result.Success(response);
    }
}
