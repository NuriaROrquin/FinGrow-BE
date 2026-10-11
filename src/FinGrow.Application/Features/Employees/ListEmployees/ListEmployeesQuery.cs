namespace FinGrow.Application.Features.Employees.ListEmployees;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListEmployeesQuery : IRequest<Result<IReadOnlyList<CompanyEmployeeResponse>>>;
