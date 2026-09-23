namespace FinGrow.Application.Features.Session;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;
