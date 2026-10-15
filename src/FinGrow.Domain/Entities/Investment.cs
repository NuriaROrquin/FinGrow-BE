namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Una posicion del portafolio del empleado: cuanto puso y cuanto vale hoy.
/// </summary>
public sealed class Investment : AggregateRoot
{
    public const int MaxAssetNameLength = 120;

    public const int MaxSymbolLength = 20;

    public const int MaxFundNameLength = 150;

    public const int MaxExternalReferenceLength = 200;

    public const string NotQuotedMessage =
        "Solo se cotizan por simbolo las acciones, los CEDEAR, los ETF, los bonos, las obligaciones negociables, "
        + "las letras, los fondos comunes y las criptomonedas.";

    public const decimal MaxAnnualRate = 999.99m;

    public const int AnnualRateDecimals = 2;

    public const int MaxTermDays = 3650;

    public const string RateRequiredMessage =
        "La TNA es obligatoria en los plazos fijos, las cauciones y las cuentas remuneradas.";

    public const string NotAccruingMessage =
        "Solo los plazos fijos, las cauciones y las cuentas remuneradas llevan TNA.";

    public const string TermRequiredMessage = "El plazo en dias es obligatorio en los plazos fijos y las cauciones.";

    public const string TermNotAllowedMessage = "Solo los plazos fijos y las cauciones llevan plazo.";

    public const string RateOutOfRangeMessage = "La TNA tiene que ser mayor a cero y no superar el 999.99%.";

    public const string RateDecimalsMessage = "La TNA admite hasta dos decimales.";

    public const string TermOutOfRangeMessage = "El plazo tiene que estar entre 1 y 3650 dias.";

    private const int DaysPerYear = 365;

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
        Source = InvestmentSource.Manual;
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

    public string? Symbol { get; private set; }

    public decimal? Quantity { get; private set; }

    public decimal? AnnualRate { get; private set; }

    public int? TermDays { get; private set; }

    public InvestmentSource Source { get; private set; }

    public string? ExternalReference { get; private set; }

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
        EnsureCanBeCreated(employeeId, investedAmount);

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
        investment.Raise(new InvestmentAdded(investment.Id, employeeId, investment.Source, createdAt));

        return investment;
    }


    public static Investment Import(
        Guid employeeId,
        InvestmentSource source,
        string externalReference,
        string assetName,
        InvestmentType type,
        string symbol,
        decimal quantity,
        Money investedAmount,
        DateOnly purchasedOn,
        Money marketValue,
        DateOnly valuedOn,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(marketValue);
        EnsureCanBeCreated(employeeId, investedAmount);

        if (source == InvestmentSource.Manual)
        {
            throw new DomainException("Una inversion importada tiene que venir de una integracion.");
        }

        var reference = EnsureValidExternalReference(externalReference);
        var investment = new Investment(
            Guid.CreateVersion7(),
            employeeId,
            EnsureValidAssetName(assetName),
            type,
            investedAmount,
            purchasedOn,
            createdAt);

        investment.Track(symbol, quantity, createdAt);
        investment.Source = source;
        investment.ExternalReference = reference;
        investment.RecordValuation(marketValue, valuedOn, ValuationSource.Feed, createdAt);
        investment.Raise(new InvestmentAdded(investment.Id, employeeId, source, createdAt));

        return investment;
    }

    public bool UpdatePosition(
        decimal quantity,
        Money investedAmount,
        Money marketValue,
        DateOnly valuedOn,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(investedAmount);
        ArgumentNullException.ThrowIfNull(marketValue);

        if (Source == InvestmentSource.Manual)
        {
            throw new DomainException("Una inversion cargada a mano no se actualiza con la posicion de una integracion.");
        }

        if (quantity <= 0m)
        {
            throw new DomainException("La cantidad tiene que ser mayor a cero.");
        }

        if (investedAmount.IsZero)
        {
            throw new DomainException("El capital invertido tiene que ser mayor a cero.");
        }

        if (investedAmount.Currency != InvestedAmount.Currency)
        {
            throw new DomainException("El capital invertido tiene que seguir en la misma moneda.");
        }

        var changed = false;

        // El broker informa el capital con su precio promedio de compra: solo una compra o una venta lo mueve.
        if (quantity != Quantity)
        {
            Quantity = quantity;
            InvestedAmount = investedAmount;
            UpdatedAt = updatedAt;
            changed = true;
        }

        if (marketValue != CurrentValue)
        {
            RecordMarketValuation(marketValue, valuedOn, updatedAt);
            changed = true;
        }

        return changed;
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

    public bool HasMarketValuation => _valuations.Any(valuation => valuation.Source != ValuationSource.Manual);

    public DateOnly? MaturesOn => TermDays is { } termDays ? PurchasedOn.AddDays(termDays) : null;

    public bool IsValuedAtMaturity => MaturesOn is { } maturesOn && ValuedOn >= maturesOn;

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

    public void Correct(
        string assetName,
        InvestmentType type,
        Money investedAmount,
        DateOnly purchasedOn,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(investedAmount);

        if (investedAmount.IsZero)
        {
            throw new DomainException("El capital invertido tiene que ser mayor a cero.");
        }

        var validAssetName = EnsureValidAssetName(assetName);

        if (Source != InvestmentSource.Manual
            && (type != Type || investedAmount != InvestedAmount || purchasedOn != PurchasedOn))
        {
            throw new DomainException(
                "De una inversion importada solo se puede cambiar el nombre: el resto lo informa la integracion.");
        }

        if (investedAmount != InvestedAmount || purchasedOn != PurchasedOn)
        {
            var recorded = _valuations.Where(valuation => valuation.Source != ValuationSource.Accrued).ToList();

            if (recorded.Count > 1)
            {
                throw new DomainException(
                    "La inversion ya tiene valuaciones posteriores a la compra: solo se puede corregir el nombre y el tipo.");
            }

            _valuations.RemoveAll(valuation => valuation.Source == ValuationSource.Accrued);
            recorded.Single().Correct(investedAmount, purchasedOn);
            InvestedAmount = investedAmount;
            PurchasedOn = purchasedOn;
        }

        AssetName = validAssetName;
        Type = type;
        UpdatedAt = updatedAt;
    }

    public void Track(string? symbol, decimal? quantity, DateTimeOffset updatedAt)
    {
        var hasSymbol = !string.IsNullOrWhiteSpace(symbol);

        if (hasSymbol != (quantity is not null))
        {
            throw new DomainException("El simbolo y la cantidad van juntos: se cargan los dos o ninguno.");
        }

        string? normalizedSymbol = null;

        if (hasSymbol)
        {
            var market = Type.QuotedOn() ?? throw new DomainException(NotQuotedMessage);
            normalizedSymbol = market.NormalizeSymbol(symbol!);

            if (normalizedSymbol.Length > market.MaxSymbolLength())
            {
                throw new DomainException($"El simbolo no puede superar los {market.MaxSymbolLength()} caracteres.");
            }
        }

        if (quantity is <= 0m)
        {
            throw new DomainException("La cantidad tiene que ser mayor a cero.");
        }

        if (Source != InvestmentSource.Manual && (normalizedSymbol != Symbol || quantity != Quantity))
        {
            throw new DomainException(
                "El simbolo y la cantidad de una inversion importada los informa la integracion: no se cambian a mano.");
        }

        Symbol = normalizedSymbol;
        Quantity = quantity;
        UpdatedAt = updatedAt;
    }

    public InvestmentValuation RecordMarketValuation(Money value, DateOnly valuedOn, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(value);

        var sameDay = _valuations.SingleOrDefault(valuation =>
            valuation.Source == ValuationSource.Feed && valuation.ValuedOn == valuedOn);

        if (sameDay is null)
        {
            return RecordValuation(value, valuedOn, ValuationSource.Feed, recordedAt);
        }

        if (value.Currency != InvestedAmount.Currency)
        {
            throw new DomainException("La valuacion tiene que estar en la misma moneda que el capital invertido.");
        }

        sameDay.Correct(value, valuedOn);
        UpdatedAt = recordedAt;

        return sameDay;
    }

    public void AgreeRate(decimal? annualRate, int? termDays, DateOnly today, DateTimeOffset updatedAt)
    {
        EnsureValidRate(annualRate, termDays);

        if (annualRate != AnnualRate || termDays != TermDays)
        {
            AnnualRate = annualRate;
            TermDays = termDays;
            _valuations.RemoveAll(valuation => valuation.Source == ValuationSource.Accrued);
            UpdatedAt = updatedAt;
        }

        Accrue(today, updatedAt);
    }

    public void Accrue(DateOnly today, DateTimeOffset recordedAt)
    {
        if (AnnualRate is not { } annualRate || today < PurchasedOn)
        {
            return;
        }

        var valuedOn = MaturesOn is { } maturesOn && maturesOn < today ? maturesOn : today;
        var days = valuedOn.DayNumber - PurchasedOn.DayNumber;
        var value = Money.From(InvestedAmount.Amount * GrowthFactor(annualRate, days), InvestedAmount.Currency);

        var sameDay = _valuations.SingleOrDefault(valuation =>
            valuation.Source == ValuationSource.Accrued && valuation.ValuedOn == valuedOn);

        if (sameDay is null)
        {
            RecordValuation(value, valuedOn, ValuationSource.Accrued, recordedAt);
            return;
        }

        if (sameDay.Value != value)
        {
            sameDay.Correct(value, valuedOn);
            UpdatedAt = recordedAt;
        }
    }

    public void Rename(string assetName, DateTimeOffset updatedAt)
    {
        AssetName = EnsureValidAssetName(assetName);
        UpdatedAt = updatedAt;
    }

    private static void EnsureCanBeCreated(Guid employeeId, Money investedAmount)
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
    }

    private void EnsureValidRate(decimal? annualRate, int? termDays)
    {
        if (annualRate is not { } rate)
        {
            if (termDays is not null)
            {
                throw new DomainException("El plazo va con la TNA: se cargan los dos o ninguno.");
            }

            if (Type.AccruesInterest() && Source == InvestmentSource.Manual)
            {
                throw new DomainException(RateRequiredMessage);
            }

            return;
        }

        if (!Type.AccruesInterest())
        {
            throw new DomainException(NotAccruingMessage);
        }

        if (rate <= 0m || rate > MaxAnnualRate)
        {
            throw new DomainException(RateOutOfRangeMessage);
        }

        if (decimal.Round(rate, AnnualRateDecimals) != rate)
        {
            throw new DomainException(RateDecimalsMessage);
        }

        if (Type.HasTerm() != termDays.HasValue)
        {
            throw new DomainException(Type.HasTerm() ? TermRequiredMessage : TermNotAllowedMessage);
        }

        if (termDays is < 1 or > MaxTermDays)
        {
            throw new DomainException(TermOutOfRangeMessage);
        }
    }

    private decimal GrowthFactor(decimal annualRate, int days)
    {
        var dailyRate = annualRate / 100m / DaysPerYear;

        return Type.CompoundsDaily() ? Power(1m + dailyRate, days) : 1m + (dailyRate * days);
    }

    private static decimal Power(decimal factor, int exponent)
    {
        var result = 1m;

        while (exponent > 0)
        {
            if ((exponent & 1) == 1)
            {
                result *= factor;
            }

            factor *= factor;
            exponent >>= 1;
        }

        return result;
    }

    private static string EnsureValidExternalReference(string externalReference)
    {
        var trimmed = (externalReference ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("Una inversion importada necesita la referencia de la integracion."),
            > MaxExternalReferenceLength => throw new DomainException(
                $"La referencia externa no puede superar los {MaxExternalReferenceLength} caracteres."),
            _ => trimmed
        };
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
