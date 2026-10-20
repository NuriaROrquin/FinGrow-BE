namespace FinGrow.Application.Features.PasswordReset;

/// <summary>El rol le dice al front a qué login volver: la empresa vuelve a /login/empresa.</summary>
public sealed record PasswordResetResponse(string Role);
