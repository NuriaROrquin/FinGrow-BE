namespace FinGrow.Application.Features.Budgets.DeleteBudget;

using Common;
using MediatR;

public sealed record DeleteBudgetCommand(Guid EmployeeId, int Year, int Month) : IRequest<Result>;
