namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Una posicion del portafolio del empleado: cuanto puso y cuanto vale hoy.
/// </summary>
public sealed class Investment : AggregateRoot
{
    public const int MaxAssetNameLength = 120;

    private Investment()
    {
    }

    private Investment(
        Guid id,
        Guid employeeId,
        string assetName,
        InvestmentType type,
        Money investedAmount,
        Money currentValue,
        DateOnly purchasedOn,
        DateTimeOffset valuedAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        AssetName = assetName;
        Type = type;
        InvestedAmount = investedAmount;
        CurrentValue = currentValue;
        PurchasedOn = purchasedOn;
        ValuedAt = valuedAt;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    /// <summary>Nombre del activo tal como lo escribe el empleado: "S&amp;P 500", "Bitcoin", "AL30".</summary>
    public string AssetName { get; private set; } = string.Empty;

    public InvestmentType Type { get; private set; }

    /// <summary>Capital invertido, el numero contra el que se mide el rendimiento.</summary>
    public Money InvestedAmount { get; private set; } = null!;

    public Money CurrentValue { get; private set; } = null!;

    public DateOnly PurchasedOn { get; private set; }

    /// <summary>Cuando se cargo la ultima valuacion. Sin esto un rendimiento no se puede interpretar.</summary>
    public DateTimeOffset ValuedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public static Investment Create(
        Guid employeeId,
        string assetName,
        InvestmentType type,
        Money investedAmount,
        DateOnly purchasedOn,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(investedAmount);

        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Una inversion siempre pertenece a un empleado.");
        }

        if (investedAmount.IsZero)
        {
            throw new DomainException("El capital invertido tiene que ser mayor a cero.");
        }

        return new Investment(
            Guid.CreateVersion7(),
            employeeId,
            EnsureValidAssetName(assetName),
            type,
            investedAmount,
            investedAmount,
            purchasedOn,
            createdAt,
            createdAt);
    }

    /// <summary>Ganancia o perdida en unidades de la moneda. Negativo es un resultado valido.</summary>
    public decimal ReturnAmount => CurrentValue.DifferenceWith(InvestedAmount);

    /// <summary>Rendimiento porcentual sobre el capital invertido.</summary>
    public decimal ReturnPercentage => InvestedAmount.IsZero
        ? 0m
        : decimal.Round(ReturnAmount / InvestedAmount.Amount * 100m, 2, MidpointRounding.ToEven);

    public void UpdateValuation(Money currentValue, DateTimeOffset valuedAt)
    {
        ArgumentNullException.ThrowIfNull(currentValue);

        if (currentValue.Currency != InvestedAmount.Currency)
        {
            throw new DomainException("La valuacion tiene que estar en la misma moneda que el capital invertido.");
        }

        CurrentValue = currentValue;
        ValuedAt = valuedAt;
        UpdatedAt = valuedAt;
    }

    public void AddCapital(Money amount, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.IsZero)
        {
            throw new DomainException("El capital agregado tiene que ser mayor a cero.");
        }

        InvestedAmount = InvestedAmount.Add(amount);
        CurrentValue = CurrentValue.Add(amount);
        UpdatedAt = updatedAt;
    }

    public void Rename(string assetName, DateTimeOffset updatedAt)
    {
        AssetName = EnsureValidAssetName(assetName);
        UpdatedAt = updatedAt;
    }

    private static string EnsureValidAssetName(string assetName)
    {
        var trimmed = (assetName ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El nombre del activo es obligatorio."),
            > MaxAssetNameLength => throw new DomainException(
                $"El nombre del activo no puede superar los {MaxAssetNameLength} caracteres."),
            _ => trimmed
        };
    }
}
