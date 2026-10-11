namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Api.Contracts;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Employees.ChangeEmployeeAccess;
using FinGrow.Application.Features.Employees.ChangeEmployeeDepartment;
using FinGrow.Application.Features.Employees.CreateEmployee;
using FinGrow.Application.Features.Employees.DeleteEmployee;
using FinGrow.Application.Features.Employees.ListEmployees;
using FinGrow.Application.Features.Employees.UpdateEmployee;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/company/employees")]
[Authorize(Roles = Rol.Empresa)]
public sealed class CompanyEmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new ListEmployeesQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    [HttpPut("{employeeId:guid}")]
    public async Task<IActionResult> Update(
        Guid employeeId,
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken) =>
        (await sender.Send(command with { EmployeeId = employeeId }, cancellationToken)).ToActionResult();

    [HttpPut("{employeeId:guid}/department")]
    public async Task<IActionResult> ChangeDepartment(
        Guid employeeId,
        ChangeEmployeeDepartmentRequest request,
        CancellationToken cancellationToken) =>
        (await sender.Send(new ChangeEmployeeDepartmentCommand(employeeId, request.DepartmentId), cancellationToken))
        .ToActionResult();

    [HttpPut("{employeeId:guid}/access")]
    public async Task<IActionResult> ChangeAccess(
        Guid employeeId,
        ChangeEmployeeAccessRequest request,
        CancellationToken cancellationToken) =>
        (await sender.Send(new ChangeEmployeeAccessCommand(employeeId, request.IsActive!.Value), cancellationToken))
        .ToActionResult();

    [HttpDelete("{employeeId:guid}")]
    public async Task<IActionResult> Delete(Guid employeeId, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteEmployeeCommand(employeeId), cancellationToken)).ToActionResult();
}
