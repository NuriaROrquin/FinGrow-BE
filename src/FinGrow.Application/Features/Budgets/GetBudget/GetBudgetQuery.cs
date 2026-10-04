namespace FinGrow.Application.Features.Budgets.GetBudget;

using Common;
using DTOs;
using MediatR;

public sealed record GetBudgetQuery(Guid EmployeeId, int Year, int Month) : IRequest<Result<BudgetResponse>>;
