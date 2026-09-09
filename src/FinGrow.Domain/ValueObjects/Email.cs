namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

/// <summary>
/// Se normaliza a minusculas al construirse: asi el indice unico de la base trata
/// "Juan@Empresa.com" y "juan@empresa.com" como la misma persona.
/// </summary>
public sealed class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email()
    {
    }

    private Email(string value) => Value = value;

    public string Value { get; private set; } = string.Empty;

    public static Email From(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Length == 0)
        {
            throw new DomainException("El email es obligatorio.");
        }

        if (normalized.Length > MaxLength)
        {
            throw new DomainException($"El email no puede superar los {MaxLength} caracteres.");
        }

        if (!IsWellFormed(normalized))
        {
            throw new DomainException($"'{value}' no es una direccion de email valida.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsWellFormed(string candidate)
    {
        var at = candidate.IndexOf('@', StringComparison.Ordinal);

        if (at <= 0 || at == candidate.Length - 1)
        {
            return false;
        }

        if (candidate.IndexOf('@', at + 1) >= 0 || candidate.Contains(' ', StringComparison.Ordinal))
        {
            return false;
        }

        var domain = candidate[(at + 1)..];
        var dot = domain.IndexOf('.', StringComparison.Ordinal);

        return dot > 0 && dot < domain.Length - 1;
    }
}
