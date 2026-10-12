namespace FinGrow.Application.Features.Integrations.Iol.Sync;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class IolSynchronizer
{
    internal static readonly IolCountry[] Countries = { IolCountry.Argentina, IolCountry.UnitedStates };

    // El portafolio no trae la fecha de compra: sale de las operaciones, y mas atras de esto se usa el dia de hoy.
    internal const int OperationsLookbackYears = 10;

    private static readonly IReadOnlyDictionary<string, DateOnly> NoPurchaseDates = new Dictionary<string, DateOnly>();

    private readonly IolAccessTokenProvider _tokens;
    private readonly IIolPortfolioClient _iol;
    private readonly IInvestmentRepository _investments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<IolSynchronizer> _logger;

    public IolSynchronizer(
        IolAccessTokenProvider tokens,
        IIolPortfolioClient iol,
        IInvestmentRepository investments,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<IolSynchronizer> logger)
    {
        _tokens = tokens;
        _iol = iol;
        _investments = investments;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<IolSyncSummary>> SyncAsync(EmployeeIntegration integration, CancellationToken cancellationToken)
    {
        var accessToken = await _tokens.GetAccessTokenAsync(integration, cancellationToken);

        if (accessToken.IsFailure)
        {
            return Result.Failure<IolSyncSummary>(accessToken.Error);
        }

        // Los dos portafolios se piden antes de tocar: si IOL falla a mitad de camino no queda una foto a medias.
        var positions = await FetchPortfolioAsync(integration.EmployeeId, accessToken.Value, cancellationToken);

        if (positions is null)
        {
            return Result.Failure<IolSyncSummary>(IolErrors.Unavailable);
        }

        var now = _clock.UtcNow;
        var today = _clock.Today;
        var mapped = positions.Select(IolPositionMapper.ToHolding).ToList();
        var holdings = IolPositionMapper.Consolidate(mapped.OfType<IolHolding>());

        var existing = (await _investments.ListImportedAsync(integration.EmployeeId, InvestmentSource.Iol, cancellationToken))
            .ToDictionary(investment => investment.ExternalReference!, StringComparer.Ordinal);

        var fresh = holdings.Where(holding => !existing.ContainsKey(holding.ExternalReference)).ToList();
        var purchaseDates = fresh.Count == 0
            ? NoPurchaseDates
            : await FirstPurchaseDatesAsync(integration.EmployeeId, accessToken.Value, today, cancellationToken);

        var updated = 0;
        var unchanged = 0;

        foreach (var holding in holdings)
        {
            if (!existing.TryGetValue(holding.ExternalReference, out var investment))
            {
                continue;
            }

            var changed = investment.UpdatePosition(
                holding.Quantity,
                Money.From(holding.InvestedAmount, holding.Currency),
                Money.From(holding.ValuedAmount, holding.Currency),
                today,
                now);

            if (changed)
            {
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        foreach (var holding in fresh)
        {
            var purchasedOn = purchaseDates.TryGetValue(holding.Symbol.ToUpperInvariant(), out var firstPurchase) && firstPurchase <= today
                ? firstPurchase
                : today;

            _investments.Add(Investment.Import(
                integration.EmployeeId,
                InvestmentSource.Iol,
                holding.ExternalReference,
                holding.AssetName,
                holding.Type,
                holding.Symbol,
                holding.Quantity,
                Money.From(holding.InvestedAmount, holding.Currency),
                purchasedOn,
                Money.From(holding.ValuedAmount, holding.Currency),
                today,
                now));
        }

        var held = holdings.Select(holding => holding.ExternalReference).ToHashSet(StringComparer.Ordinal);
        var noLongerHeld = existing.Keys.Count(reference => !held.Contains(reference));

        integration.MarkSynced(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = new IolSyncSummary(fresh.Count, updated, unchanged, mapped.Count(holding => holding is null), noLongerHeld, now);
        LogSynced(_logger, integration.EmployeeId, summary.Imported, summary.Updated, summary.Unchanged, summary.Ignored, summary.NoLongerHeld);

        return Result.Success(summary);
    }

    private async Task<List<IolPosition>?> FetchPortfolioAsync(Guid employeeId, string accessToken, CancellationToken cancellationToken)
    {
        var positions = new List<IolPosition>();

        try
        {
            foreach (var country in Countries)
            {
                positions.AddRange(await _iol.GetPortfolioAsync(accessToken, country, cancellationToken));
            }

            return positions;
        }
        catch (HttpRequestException exception)
        {
            LogPortfolioUnavailable(_logger, exception, employeeId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogPortfolioUnavailable(_logger, exception, employeeId);
        }

        return null;
    }

    private async Task<IReadOnlyDictionary<string, DateOnly>> FirstPurchaseDatesAsync(
        Guid employeeId,
        string accessToken,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        try
        {
            var operations = await _iol.ListCompletedOperationsAsync(
                accessToken,
                today.AddYears(-OperationsLookbackYears),
                cancellationToken);

            return IolPositionMapper.FirstPurchaseDates(operations);
        }
        catch (HttpRequestException exception)
        {
            LogOperationsUnavailable(_logger, exception, employeeId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogOperationsUnavailable(_logger, exception, employeeId);
        }

        return NoPurchaseDates;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "IOL sincronizado para el empleado {EmployeeId}: {Imported} nuevas, {Updated} actualizadas, {Unchanged} sin cambios, {Ignored} ignoradas, {NoLongerHeld} que ya no estan en IOL.")]
    private static partial void LogSynced(ILogger logger, Guid employeeId, int imported, int updated, int unchanged, int ignored, int noLongerHeld);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IOL no devolvio el portafolio del empleado {EmployeeId}: no se toco ninguna inversion y la proxima corrida lo reintenta.")]
    private static partial void LogPortfolioUnavailable(ILogger logger, Exception exception, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IOL no devolvio las operaciones del empleado {EmployeeId}: las inversiones nuevas toman como fecha de compra el dia de hoy.")]
    private static partial void LogOperationsUnavailable(ILogger logger, Exception exception, Guid employeeId);
}
