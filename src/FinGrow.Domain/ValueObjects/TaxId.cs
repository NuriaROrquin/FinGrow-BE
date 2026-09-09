namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

/// <summary>
/// CUIT de la empresa contratante. Los once digitos traen su propio digito verificador,
/// asi que un CUIT mal tipeado se detecta aca y no tres pantallas mas adelante.
/// </summary>
public sealed class TaxId : ValueObject
{
    public const int Length = 11;

    private static readonly int[] Weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    private TaxId()
    {
    }

    private TaxId(string value) => Value = value;

    /// <summary>Once digitos, sin guiones ni puntos.</summary>
    public string Value { get; private set; } = string.Empty;

    public static TaxId From(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        if (digits.Length != Length)
        {
            throw new DomainException($"El CUIT debe tener {Length} digitos.");
        }

        if (ExpectedCheckDigit(digits) != digits[Length - 1] - '0')
        {
            throw new DomainException($"El CUIT '{value}' no es valido: el digito verificador no coincide.");
        }

        return new TaxId(digits);
    }

    /// <summary>Formato de lectura: 30-71234567-8.</summary>
    public string ToDisplayString() => $"{Value[..2]}-{Value[2..10]}-{Value[10..]}";

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static int ExpectedCheckDigit(string digits)
    {
        var sum = 0;

        for (var i = 0; i < Weights.Length; i++)
        {
            sum += (digits[i] - '0') * Weights[i];
        }

        var remainder = 11 - (sum % 11);

        return remainder switch
        {
            11 => 0,
            10 => 9,
            _ => remainder
        };
    }
}
