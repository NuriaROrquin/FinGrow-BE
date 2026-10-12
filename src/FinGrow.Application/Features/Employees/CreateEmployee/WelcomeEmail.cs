namespace FinGrow.Application.Features.Employees.CreateEmployee;

using System.Net;
using FinGrow.Domain.Entities;

internal static class WelcomeEmail
{
    public const string Subject = "Tu acceso a FinGrow";

    public static OutboundEmail Queue(Employee employee, Company company, string temporaryPassword, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(company);

        var text = $"""
            Hola {employee.FullName},

            {company.Name} te dio de alta en FinGrow. Para entrar usá estos datos:

            Email: {employee.Email.Value}
            Contraseña temporal: {temporaryPassword}

            Te recomendamos cambiar la contraseña desde tu perfil la primera vez que entres.
            """;

        var html = $"""
            <p>Hola {Encode(employee.FullName)},</p>
            <p>{Encode(company.Name)} te dio de alta en FinGrow. Para entrar usá estos datos:</p>
            <p>Email: <strong>{Encode(employee.Email.Value)}</strong><br>
            Contraseña temporal: <strong>{Encode(temporaryPassword)}</strong></p>
            <p>Te recomendamos cambiar la contraseña desde tu perfil la primera vez que entres.</p>
            """;

        return OutboundEmail.Queue(employee.Email, Subject, html, text, queuedAt);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
