namespace FinGrow.Application.Features.Departments.ChangeDepartmentStatus;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ChangeDepartmentStatusCommand(Guid DepartmentId, bool IsActive, Guid? MoveEmployeesTo)
    : IRequest<Result<DepartmentResponse>>;
