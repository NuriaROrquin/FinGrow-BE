namespace FinGrow.Application.Features.Departments.ListDepartments;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListDepartmentsQuery : IRequest<Result<IReadOnlyList<DepartmentResponse>>>;
