namespace FinGrow.Application.Features.Goals.CreateGoal;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record CreateGoalCommand(
    Guid EmployeeId,
    string Name,
    decimal TargetAmount,
    Currency Currency,
    DateOnly Deadline) : IRequest<Result<GoalResponse>>;
