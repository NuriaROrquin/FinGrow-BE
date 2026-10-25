namespace FinGrow.Application.Features.PasswordReset;

using FinGrow.Application.Common;

internal static class PasswordResetErrors
{
    // Vencido, usado o inexistente responden lo mismo: distinguirlos no le sirve a quien abre el
    // enlace y sí a quien prueba tokens al azar.
    public static readonly Error EnlaceInvalido =
        Error.Validation("PasswordReset.EnlaceInvalido", "El enlace para restablecer la contraseña venció, ya se usó o no es válido.");
}
