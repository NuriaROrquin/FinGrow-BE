namespace FinGrow.Application.Features.Achievements.ListUncelebratedAchievements;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListUncelebratedAchievementsQuery(Guid EmployeeId) : IRequest<Result<IReadOnlyList<AchievementResponse>>>;
