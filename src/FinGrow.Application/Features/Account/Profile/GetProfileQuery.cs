namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record GetProfileQuery : IRequest<Result<EmployeeProfileResponse>>;
