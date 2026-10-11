namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
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

    [HttpDelete("{employeeId:guid}")]
    public async Task<IActionResult> Delete(Guid employeeId, CancellationToken cancellationToken) =>
        (await sender.Send(new DeleteEmployeeCommand(employeeId), cancellationToken)).ToActionResult();
}
