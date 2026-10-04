namespace FinGrow.Application.Features.Goals.UpdateGoal;

using Common;
using DTOs;
using MediatR;

public sealed record UpdateGoalCommand(
    Guid EmployeeId,
    Guid GoalId,
    string Name,
    decimal TargetAmount,
    DateOnly Deadline) : IRequest<Result<GoalResponse>>;
