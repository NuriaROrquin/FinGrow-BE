namespace FinGrow.Application.Features.Account.ChangePassword;

using FinGrow.Application.Common;
using MediatR;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;
