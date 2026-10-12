namespace FinGrow.Application.Features.Employees.CreateEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record CreateEmployeeCommand(string FullName, string Email, string? PhoneNumber, Guid? DepartmentId)
    : IRequest<Result<CreatedEmployeeResponse>>, IEmployeeDetails;
