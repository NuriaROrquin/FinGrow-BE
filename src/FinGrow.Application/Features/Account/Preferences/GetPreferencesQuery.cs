namespace FinGrow.Application.Features.Account.Preferences;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record GetPreferencesQuery : IRequest<Result<EmployeePreferencesResponse>>;
