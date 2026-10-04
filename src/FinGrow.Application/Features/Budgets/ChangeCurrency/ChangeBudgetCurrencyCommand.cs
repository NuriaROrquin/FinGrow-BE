namespace FinGrow.Application.Features.Budgets.ChangeCurrency;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record ChangeBudgetCurrencyCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    Currency Currency) : IRequest<Result<BudgetResponse>>;
