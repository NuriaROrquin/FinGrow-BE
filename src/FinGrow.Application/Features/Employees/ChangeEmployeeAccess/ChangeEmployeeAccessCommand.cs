namespace FinGrow.Application.Features.Employees.ChangeEmployeeAccess;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ChangeEmployeeAccessCommand(Guid EmployeeId, bool IsActive)
    : IRequest<Result<CompanyEmployeeResponse>>;
