namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;

internal static class TwoFactorErrors
{
    public static readonly Error NoAutenticado =
        Error.Unauthorized("Account.NoAutenticado", "Hay que iniciar sesión para configurar el doble factor.");

    public static readonly Error YaActivo =
        Error.Conflict("Account.DobleFactorYaActivo", "El doble factor ya está activo en tu cuenta.");

    public static readonly Error AltaNoIniciada =
        Error.Conflict("Account.DobleFactorSinAlta", "Primero hay que generar el código QR para configurar la app de autenticación.");

    public static readonly Error CodigoInvalido =
        Error.Validation("Account.CodigoInvalido", "El código de verificación no es correcto.");
}
