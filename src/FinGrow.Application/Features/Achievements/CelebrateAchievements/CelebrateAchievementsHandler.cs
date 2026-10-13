namespace FinGrow.Application.Features.Achievements.CelebrateAchievements;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class CelebrateAchievementsHandler(
    IAchievementRepository achievements,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<CelebrateAchievementsCommand, Result<CelebratedAchievementsResponse>>
{
    public async Task<Result<CelebratedAchievementsResponse>> Handle(
        CelebrateAchievementsCommand request,
        CancellationToken cancellationToken)
    {
        var codes = request.Codes.ToHashSet();
        var celebrated = (await achievements.ListUncelebratedAsync(request.EmployeeId, cancellationToken))
            .Where(achievement => codes.Contains(achievement.Code))
            .ToList();

        if (celebrated.Count == 0)
        {
            return Result.Success(new CelebratedAchievementsResponse(0));
        }

        var now = clock.UtcNow;

        foreach (var achievement in celebrated)
        {
            achievement.MarkCelebrated(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CelebratedAchievementsResponse(celebrated.Count));
    }
}
