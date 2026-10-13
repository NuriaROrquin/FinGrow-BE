namespace FinGrow.Api.Controllers;

using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.CompanyPreferences.GetCompanyPreferences;
using FinGrow.Application.Features.CompanyPreferences.UpdateCompanyPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/company/preferences")]
[Authorize(Roles = Rol.Empresa)]
public sealed class CompanyPreferencesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CompanyPreferencesResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyPreferencesQuery(), cancellationToken)).ToActionResult();

    [HttpPut]
    [ProducesResponseType<CompanyPreferencesResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        UpdateCompanyPreferencesCommand command,
        CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();
}
