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

    private readonly List<InvestmentValuation> _valuations = new();

    private Investment()
    {
    }

    private Investment(
        Guid id,
        Guid employeeId,
        string assetName,
        InvestmentType type,
        Money investedAmount,
        DateOnly purchasedOn,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        AssetName = assetName;
        Type = type;
        InvestedAmount = investedAmount;
        PurchasedOn = purchasedOn;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    /// <summary>Nombre del activo tal como lo escribe el empleado: "S&amp;P 500", "Bitcoin", "AL30".</summary>
    public string AssetName { get; private set; } = string.Empty;

    public InvestmentType Type { get; private set; }

    /// <summary>Capital invertido, el numero contra el que se mide el rendimiento.</summary>
    public Money InvestedAmount { get; private set; } = null!;

    public DateOnly PurchasedOn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public IReadOnlyCollection<InvestmentValuation> Valuations => _valuations.AsReadOnly();

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

        var investment = new Investment(
            Guid.CreateVersion7(),
            employeeId,
            EnsureValidAssetName(assetName),
            type,
            investedAmount,
            purchasedOn,
            createdAt);

        // El dia de la compra vale lo que costo: asi siempre hay un ultimo valor conocido (HU-32).
        investment._valuations.Add(InvestmentValuation.Create(
            investment.Id,
            investedAmount,
            purchasedOn,
            ValuationSource.Manual,
            createdAt));

        return investment;
    }

    public InvestmentValuation LatestValuation => _valuations
        .OrderByDescending(valuation => valuation.ValuedOn)
        .ThenByDescending(valuation => valuation.CreatedAt)
        .First();

    public Money CurrentValue => LatestValuation.Value;

    public DateOnly ValuedOn => LatestValuation.ValuedOn;

    /// <summary>Ganancia o perdida en unidades de la moneda. Negativo es un resultado valido.</summary>
    public decimal ReturnAmount => CurrentValue.DifferenceWith(InvestedAmount);

    /// <summary>Rendimiento porcentual sobre el capital invertido.</summary>
    public decimal ReturnPercentage => InvestedAmount.IsZero
        ? 0m
        : decimal.Round(ReturnAmount / InvestedAmount.Amount * 100m, 2, MidpointRounding.ToEven);

    public InvestmentValuation RecordValuation(
        Money value,
        DateOnly valuedOn,
        ValuationSource source,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Currency != InvestedAmount.Currency)
        {
            throw new DomainException("La valuacion tiene que estar en la misma moneda que el capital invertido.");
        }

        if (valuedOn < PurchasedOn)
        {
            throw new DomainException("Una inversion no puede valuarse antes de haberse comprado.");
        }

        var valuation = InvestmentValuation.Create(Id, value, valuedOn, source, recordedAt);
        _valuations.Add(valuation);
        UpdatedAt = recordedAt;

        return valuation;
    }

    public void AddCapital(Money amount, DateOnly addedOn, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.IsZero)
        {
            throw new DomainException("El capital agregado tiene que ser mayor a cero.");
        }

        if (addedOn < PurchasedOn)
        {
            throw new DomainException("No se puede agregar capital antes de la compra.");
        }

        var valueAfterContribution = CurrentValue.Add(amount);

        InvestedAmount = InvestedAmount.Add(amount);
        _valuations.Add(InvestmentValuation.Create(
            Id,
            valueAfterContribution,
            addedOn,
            ValuationSource.Manual,
            updatedAt));
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
