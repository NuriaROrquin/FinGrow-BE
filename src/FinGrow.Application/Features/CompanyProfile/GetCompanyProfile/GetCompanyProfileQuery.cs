namespace FinGrow.Application.Features.CompanyProfile.GetCompanyProfile;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record GetCompanyProfileQuery : IRequest<Result<CompanyProfileResponse>>;
