namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

/// <summary>
/// Un importe siempre viene pegado a su moneda. Guardar el numero suelto es lo que permite
/// que en algun lado se sumen 1000 pesos con 1000 dolares sin que nada se queje.
/// </summary>
public sealed class Money : ValueObject
{
    public const int DecimalPlaces = 2;

    private Money()
    {
    }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; }

    public static Money From(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Un importe no puede ser negativo.");
        }

        if (!Enum.IsDefined(currency))
        {
            throw new DomainException($"La moneda '{currency}' no esta soportada.");
        }

        return new Money(decimal.Round(amount, DecimalPlaces, MidpointRounding.ToEven), currency);
    }

    public static Money Zero(Currency currency) => From(0m, currency);

    public bool IsZero => Amount == 0m;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        if (other.Amount > Amount)
        {
            throw new DomainException("El resultado de la resta seria un importe negativo.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Diferencia con signo, en unidades de la moneda. Se usa para rendimientos y desvios,
    /// donde el resultado negativo es informacion valida y no un error.
    /// </summary>
    public decimal DifferenceWith(Money other)
    {
        EnsureSameCurrency(other);
        return Amount - other.Amount;
    }

    public bool IsAtLeast(Money other)
    {
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    /// <summary>Que porcentaje de <paramref name="total"/> representa este importe.</summary>
    public decimal PercentageOf(Money total)
    {
        EnsureSameCurrency(total);

        return total.Amount == 0m
            ? 0m
            : decimal.Round(Amount / total.Amount * 100m, DecimalPlaces, MidpointRounding.ToEven);
    }

    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new DomainException($"No se pueden combinar importes en {Currency} y en {other.Currency}.");
        }
    }
}
