namespace FinGrow.Application.Features.Goals.ListGoals;

using Common;
using DTOs;
using MediatR;

public sealed record ListGoalsQuery(Guid EmployeeId) : IRequest<Result<IReadOnlyList<GoalResponse>>>;
