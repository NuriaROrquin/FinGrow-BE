namespace FinGrow.Application.Features.Metrics.GetParticipation;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record GetParticipationQuery : IRequest<Result<CompanyParticipationResponse>>;
