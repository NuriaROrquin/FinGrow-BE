namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResult>>;
