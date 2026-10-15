namespace FinGrow.Application.Features.CompanyProfile.UpdateCompanyProfile;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record UpdateCompanyProfileCommand(
    string LegalName,
    string TaxId,
    string Email,
    string? Industry,
    string? CorporatePhone,
    string? FiscalAddress,
    int? FoundedYear) : IRequest<Result<CompanyProfileResponse>>;
