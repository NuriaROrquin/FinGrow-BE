namespace FinGrow.Application.Features.Departments.CreateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record CreateDepartmentCommand(string Name, string? Description, string? ManagerName)
    : IRequest<Result<DepartmentResponse>>, IDepartmentDetails;
