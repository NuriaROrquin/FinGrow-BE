namespace FinGrow.Application.Features.PasswordReset.RequestPasswordReset;

using System.Net;
using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;

internal static class PasswordResetEmail
{
    public const string Subject = "Restablecé tu contraseña de FinGrow";

    public static OutboundEmail Queue(Email recipient, string name, string resetLink, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        var minutes = (int)PasswordResetToken.Lifetime.TotalMinutes;

        var text = $"""
            Hola {name},

            Pediste restablecer tu contraseña de FinGrow. Entrá a este enlace para elegir una nueva:

            {resetLink}

            El enlace sirve una sola vez y vence en {minutes} minutos.
            Si no fuiste vos, ignorá este correo: tu contraseña no cambia.
            """;

        var html = $"""
            <p>Hola {Encode(name)},</p>
            <p>Pediste restablecer tu contraseña de FinGrow. Entrá a este enlace para elegir una nueva:</p>
            <p><a href="{Encode(resetLink)}">Restablecer mi contraseña</a></p>
            <p>El enlace sirve una sola vez y vence en {minutes} minutos.<br>
            Si no fuiste vos, ignorá este correo: tu contraseña no cambia.</p>
            """;

        return OutboundEmail.Queue(recipient, Subject, html, text, queuedAt);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
