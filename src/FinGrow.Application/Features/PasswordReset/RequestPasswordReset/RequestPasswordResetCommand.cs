namespace FinGrow.Application.Features.PasswordReset.RequestPasswordReset;

using FinGrow.Application.Common;
using MediatR;

public sealed record RequestPasswordResetCommand(string Email) : IRequest<Result>;
