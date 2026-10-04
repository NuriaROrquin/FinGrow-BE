namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using MediatR;

public sealed record EnableTwoFactorCommand(string Code) : IRequest<Result>;
