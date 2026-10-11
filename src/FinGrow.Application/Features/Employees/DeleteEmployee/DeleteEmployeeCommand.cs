namespace FinGrow.Application.Features.Employees.DeleteEmployee;

using FinGrow.Application.Common;
using MediatR;

public sealed record DeleteEmployeeCommand(Guid EmployeeId) : IRequest<Result>;
