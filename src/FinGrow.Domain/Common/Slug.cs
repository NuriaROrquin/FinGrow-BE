namespace FinGrow.Domain.Common;

using System.Text.RegularExpressions;
using FinGrow.Domain.Errors;

/// <summary>
/// Clave natural del contenido educativo (<c>fundamentos-finanzas-personales</c>). Es lo que
/// permite que la carga del catalogo reconozca lo que ya existe y no lo duplique.
/// </summary>
public static partial class Slug
{
    public const int MaxLength = 120;

    public static string EnsureValid(string slug)
    {
        var trimmed = (slug ?? string.Empty).Trim();

        if (trimmed.Length is 0 or > MaxLength || !SlugPattern().IsMatch(trimmed))
        {
            throw new DomainException(
                $"El identificador '{trimmed}' no es valido: solo minusculas, numeros y guiones, hasta {MaxLength} caracteres.");
        }

        return trimmed;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
