namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using MediatR;

public sealed record DisableTwoFactorCommand(string Code) : IRequest<Result>;
