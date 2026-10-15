namespace FinGrow.Api.Controllers;

using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Departments.ChangeDepartmentStatus;
using FinGrow.Application.Features.Departments.CreateDepartment;
using FinGrow.Application.Features.Departments.DeleteDepartment;
using FinGrow.Application.Features.Departments.ListDepartments;
using FinGrow.Application.Features.Departments.UpdateDepartment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/company/departments")]
[Authorize(Roles = Rol.Empresa)]
public sealed class CompanyDepartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new ListDepartmentsQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [HttpPut("{departmentId:guid}")]
    public async Task<IActionResult> Update(
        Guid departmentId,
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken) =>
        (await sender.Send(command with { DepartmentId = departmentId }, cancellationToken)).ToActionResult();

    [HttpPut("{departmentId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid departmentId,
        ChangeDepartmentStatusRequest request,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new ChangeDepartmentStatusCommand(departmentId, request.IsActive!.Value, request.MoveEmployeesTo),
            cancellationToken)).ToActionResult();

    [HttpDelete("{departmentId:guid}")]
    public async Task<IActionResult> Delete(Guid departmentId, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteDepartmentCommand(departmentId), cancellationToken)).ToActionResult();
}
