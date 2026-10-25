namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record LoginCommand(string Email, string Password, UserRole Role) : IRequest<Result<LoginResult>>;
