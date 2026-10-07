namespace FinGrow.Application.Features.Integrations.MercadoPago.Sync;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class MercadoPagoSynchronizer
{
    internal static readonly TimeSpan InitialLookback = TimeSpan.FromDays(90);
    internal static readonly TimeSpan Overlap = TimeSpan.FromHours(1);
    internal static readonly TimeSpan RefreshWindow = TimeSpan.FromDays(7);
    internal const int MaxPagesPerRun = 20;

    internal static readonly Error NotAuthorized = Error.NotFound(
        "Integrations.MercadoPago.NotLinked", "No tenes Mercado Pago vinculado.");

    internal static readonly Error GrantExpired = Error.Failure(
        "Integrations.MercadoPago.GrantExpired", "La autorizacion de Mercado Pago vencio: volve a vincular la cuenta.");

    private readonly IMercadoPagoPaymentsClient _payments;
    private readonly IMercadoPagoOAuthClient _oauth;
    private readonly IAiService _ai;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<MercadoPagoSynchronizer> _logger;

    public MercadoPagoSynchronizer(
        IMercadoPagoPaymentsClient payments,
        IMercadoPagoOAuthClient oauth,
        IAiService ai,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<MercadoPagoSynchronizer> logger)
    {
        _payments = payments;
        _oauth = oauth;
        _ai = ai;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<MercadoPagoSyncSummary>> SyncAsync(EmployeeIntegration integration, CancellationToken cancellationToken)
    {
        if (integration.Provider != IntegrationProvider.MercadoPago || integration.Grant is null)
        {
            return Result.Failure<MercadoPagoSyncSummary>(NotAuthorized);
        }

        var now = _clock.UtcNow;
        var grant = await EnsureFreshGrantAsync(integration, now, cancellationToken);

        if (grant is null)
        {
            return Result.Failure<MercadoPagoSyncSummary>(GrantExpired);
        }

        var from = (integration.LastSyncedAt ?? now.Subtract(InitialLookback)).Subtract(Overlap);
        var payments = await FetchReauthorizingOnceAsync(integration, grant, from, now, cancellationToken);

        if (payments is null)
        {
            return Result.Failure<MercadoPagoSyncSummary>(GrantExpired);
        }

        var importable = payments.Where(MercadoPagoPaymentMapper.IsImportable).ToList();
        var known = await _transactions.ListExistingExternalReferencesAsync(
            integration.EmployeeId,
            TransactionSource.MercadoPago,
            importable.Select(MercadoPagoPaymentMapper.ExternalReference).ToList(),
            cancellationToken);

        var fresh = importable
            .Where(payment => !known.Contains(MercadoPagoPaymentMapper.ExternalReference(payment)))
            .DistinctBy(MercadoPagoPaymentMapper.ExternalReference)
            .Select(payment => MercadoPagoPaymentMapper.ToPendingTransaction(payment, integration, now))
            .ToList();

        await CategorizeAsync(fresh, now, cancellationToken);

        foreach (var transaction in fresh)
        {
            _transactions.Add(transaction);
        }

        integration.MarkSynced(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = new MercadoPagoSyncSummary(fresh.Count, importable.Count - fresh.Count, payments.Count - importable.Count, now);
        LogSynced(_logger, integration.EmployeeId, summary.Imported, summary.AlreadyKnown, summary.Ignored);

        return Result.Success(summary);
    }

    private async Task<OAuthGrant?> EnsureFreshGrantAsync(EmployeeIntegration integration, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var grant = integration.Grant!;

        if (!grant.ExpiresWithin(RefreshWindow, now))
        {
            return grant;
        }

        var renewed = await TryRefreshAsync(integration, grant, now, cancellationToken);

        if (renewed is not null)
        {
            return renewed;
        }

        return grant.ExpiresWithin(TimeSpan.Zero, now) ? null : grant;
    }

    private async Task<OAuthGrant?> TryRefreshAsync(EmployeeIntegration integration, OAuthGrant grant, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!grant.CanRefresh)
        {
            return null;
        }

        try
        {
            var refreshed = await _oauth.RefreshAsync(grant.RefreshToken!, cancellationToken);
            var renewed = OAuthGrant.From(refreshed.AccessToken, refreshed.RefreshToken, now.Add(refreshed.ExpiresIn));

            integration.Authorize(renewed, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return renewed;
        }
        catch (HttpRequestException exception)
        {
            LogRefreshFailed(_logger, exception, integration.EmployeeId);

            return null;
        }
    }

    private async Task<List<MercadoPagoPayment>?> FetchReauthorizingOnceAsync(
        EmployeeIntegration integration,
        OAuthGrant grant,
        DateTimeOffset from,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            return await FetchAsync(grant.AccessToken, from, now, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            LogUnauthorized(_logger, integration.EmployeeId);
        }

        var renewed = await TryRefreshAsync(integration, grant, now, cancellationToken);

        if (renewed is null)
        {
            return null;
        }

        try
        {
            return await FetchAsync(renewed.AccessToken, from, now, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            LogStillUnauthorized(_logger, integration.EmployeeId);

            return null;
        }
    }

    private async Task<List<MercadoPagoPayment>> FetchAsync(string accessToken, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var all = new List<MercadoPagoPayment>();

        for (var page = 0; page < MaxPagesPerRun; page++)
        {
            var result = await _payments.SearchUpdatedBetweenAsync(accessToken, from, to, all.Count, cancellationToken);

            all.AddRange(result.Results);

            if (result.Results.Count == 0 || all.Count >= result.Total)
            {
                break;
            }
        }

        return all;
    }

    private async Task CategorizeAsync(List<Transaction> transactions, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expenses = transactions.Where(transaction => transaction.Type == TransactionType.Expense).ToList();

        if (expenses.Count == 0)
        {
            return;
        }

        var byId = expenses.ToDictionary(transaction => transaction.Id);
        var request = expenses
            .Select(transaction => new ExpenseToCategorize(
                transaction.Id,
                transaction.Description,
                -transaction.Amount.Amount,
                transaction.Amount.Currency,
                transaction.OccurredOn))
            .ToList();

        try
        {
            foreach (var categorized in await _ai.CategorizeExpensesAsync(request, cancellationToken))
            {
                if (byId.TryGetValue(categorized.Id, out var transaction) && ConfidenceScale.IsValid(categorized.Confidence))
                {
                    transaction.SuggestExpenseCategory(categorized.Category, categorized.Confidence, categorized.Model, now);
                }
            }
        }
        catch (HttpRequestException exception)
        {
            LogCategorizationFailed(_logger, exception, expenses.Count);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogCategorizationFailed(_logger, exception, expenses.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Mercado Pago sincronizado para el empleado {EmployeeId}: {Imported} nuevos, {AlreadyKnown} ya cargados, {Ignored} ignorados.")]
    private static partial void LogSynced(ILogger logger, Guid employeeId, int imported, int alreadyKnown, int ignored);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo renovar el token de Mercado Pago del empleado {EmployeeId}.")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mercado Pago rechazo el token del empleado {EmployeeId} (401); se intenta renovar la autorizacion antes de darla por vencida.")]
    private static partial void LogUnauthorized(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mercado Pago sigue rechazando al empleado {EmployeeId} despues de renovar el token: la autorizacion fue revocada y hay que volver a vincular la cuenta.")]
    private static partial void LogStillUnauthorized(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FinGrow-AI no categorizo {Count} gastos de Mercado Pago; quedan como 'otros' hasta que el empleado los revise.")]
    private static partial void LogCategorizationFailed(ILogger logger, Exception exception, int count);
}
