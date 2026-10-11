namespace FinGrow.Application.Features.Employees.UpdateEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record UpdateEmployeeCommand(Guid EmployeeId, string FullName, string Email, string? PhoneNumber)
    : IRequest<Result<CompanyEmployeeResponse>>, IEmployeeDetails;
