namespace FinGrow.Application.Features.Achievements.ListAchievements;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListAchievementsQuery(Guid EmployeeId) : IRequest<Result<IReadOnlyList<AchievementResponse>>>;
