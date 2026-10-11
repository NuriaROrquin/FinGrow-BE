namespace FinGrow.Application.Features.Departments.DeleteDepartment;

using FinGrow.Application.Common;
using MediatR;

public sealed record DeleteDepartmentCommand(Guid DepartmentId) : IRequest<Result>;
