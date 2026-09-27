namespace FinGrow.Domain.Common;

using FinGrow.Domain.Errors;

internal static class RequiredText
{
    /// <summary>Recorta el texto y exige que no quede vacio ni supere el largo maximo.</summary>
    public static string Ensure(string value, int maxLength, string label)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException($"{label} es obligatorio.");
        }

        return trimmed.Length > maxLength
            ? throw new DomainException($"{label} no puede superar los {maxLength} caracteres.")
            : trimmed;
    }
}
