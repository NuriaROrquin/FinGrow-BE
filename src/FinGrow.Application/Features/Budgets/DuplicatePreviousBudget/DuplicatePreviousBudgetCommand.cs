namespace FinGrow.Application.Features.Budgets.DuplicatePreviousBudget;

using Common;
using DTOs;
using MediatR;

/// <summary>
/// Crea el presupuesto de <see cref="Year"/>/<see cref="Month"/> copiando los topes del mes anterior.
/// </summary>
public sealed record DuplicatePreviousBudgetCommand(
    Guid EmployeeId,
    int Year,
    int Month) : IRequest<Result<BudgetResponse>>;
