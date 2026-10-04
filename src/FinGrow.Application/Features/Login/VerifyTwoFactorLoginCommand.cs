namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record VerifyTwoFactorLoginCommand(string ChallengeToken, string Code) : IRequest<Result<LoginResponse>>;
