namespace FinGrow.Application.Features.PasswordReset;

using System.ComponentModel.DataAnnotations;
using FinGrow.Application.Common;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>La pantalla del front que recibe el enlace, con <c>?token=...&amp;tipo=empleado|empresa</c>.</summary>
    [Required(ErrorMessage = "Falta configurar la pantalla del front que restablece la contrasena (PasswordReset:ResetUrl).")]
    [Url]
    public string ResetUrl { get; init; } = string.Empty;

    // El tipo solo le dice al front a qué formulario mandar a pedir otro enlace si este ya no sirve;
    // el rol que vale es el que guarda el token.
    public string LinkFor(string token, string role)
    {
        var separator = ResetUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var tipo = role == Rol.Empresa ? "empresa" : "empleado";
        return $"{ResetUrl}{separator}token={Uri.EscapeDataString(token)}&tipo={tipo}";
    }
}
