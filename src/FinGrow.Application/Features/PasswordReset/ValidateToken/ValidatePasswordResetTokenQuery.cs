namespace FinGrow.Application.Features.PasswordReset.ValidateToken;

using FinGrow.Application.Common;
using MediatR;

/// <summary>Lo pide el front al abrir el enlace, para avisar que no sirve antes de que escriban la contraseña.</summary>
public sealed record ValidatePasswordResetTokenQuery(string Token) : IRequest<Result<PasswordResetResponse>>;
