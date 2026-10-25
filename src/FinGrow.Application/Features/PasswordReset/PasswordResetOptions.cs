namespace FinGrow.Application.Features.PasswordReset;

using System.ComponentModel.DataAnnotations;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>La pantalla del front que recibe el enlace, con <c>?token=...</c>.</summary>
    [Required(ErrorMessage = "Falta configurar la pantalla del front que restablece la contrasena (PasswordReset:ResetUrl).")]
    [Url]
    public string ResetUrl { get; init; } = string.Empty;

    public string LinkFor(string token)
    {
        var separator = ResetUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{ResetUrl}{separator}token={Uri.EscapeDataString(token)}";
    }
}
