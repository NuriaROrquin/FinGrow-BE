namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record SetupTwoFactorCommand : IRequest<Result<TwoFactorSetupResponse>>;
