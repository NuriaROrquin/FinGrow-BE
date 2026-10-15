namespace FinGrow.Api.Controllers;

using FinGrow.Api.Contracts;
using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.CompanyProfile.GetCompanyProfile;
using FinGrow.Application.Features.CompanyProfile.UpdateCompanyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/company/profile")]
[Authorize(Roles = Rol.Empresa)]
public sealed class CompanyProfileController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CompanyProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await sender.Send(new GetCompanyProfileQuery(), cancellationToken)).ToActionResult();

    [HttpPut]
    [ProducesResponseType<CompanyProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        UpdateCompanyProfileCommand command,
        CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();
}
