namespace FinGrow.Application.Features.Goals.DeleteGoal;

using Common;
using MediatR;

public sealed record DeleteGoalCommand(Guid EmployeeId, Guid GoalId) : IRequest<Result>;
