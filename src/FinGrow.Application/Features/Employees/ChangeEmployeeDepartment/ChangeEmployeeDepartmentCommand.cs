namespace FinGrow.Application.Features.Employees.ChangeEmployeeDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ChangeEmployeeDepartmentCommand(Guid EmployeeId, Guid? DepartmentId)
    : IRequest<Result<CompanyEmployeeResponse>>;
