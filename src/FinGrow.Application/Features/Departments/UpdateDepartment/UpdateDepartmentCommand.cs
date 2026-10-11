namespace FinGrow.Application.Features.Departments.UpdateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record UpdateDepartmentCommand(Guid DepartmentId, string Name, string? Description, string? ManagerName)
    : IRequest<Result<DepartmentResponse>>, IDepartmentDetails;
