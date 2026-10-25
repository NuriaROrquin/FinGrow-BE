namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record VerifyTwoFactorLoginCommand(string ChallengeToken, string Code, UserRole Role) : IRequest<Result<LoginResponse>>;
