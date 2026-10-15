namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class GmailSynchronizer
{
    internal static readonly TimeSpan InitialLookback = TimeSpan.FromDays(30);
    internal static readonly TimeSpan Overlap = TimeSpan.FromMinutes(10);
    internal static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);
    internal const int MaxMessagesListed = 200;
    internal const int MaxMessagesPerRun = 50;
    internal const int ParallelReads = 5;
    internal const int MaxSubjectLength = 300;
    internal const int MaxBodyLength = 8000;

    internal static readonly Error NotLinked = Error.NotFound(
        "Integrations.Gmail.NotLinked", "No tenes Gmail vinculado.");

    internal static readonly Error AccessLost = Error.Conflict(
        "Integrations.Gmail.AccessLost", "FinGrow perdio el acceso a tu Gmail: volve a autorizarlo.");

    // Lo ve el empleado en la card de Gmail.
    internal const string AccessLostReason =
        "Google rechazo el acceso a tu Gmail: quitaste el permiso o vencio. Volve a autorizarlo.";

    internal static readonly Error AiUnavailable = Error.Unavailable(
        "Integrations.Gmail.AiUnavailable", "No se pudieron interpretar los mails: FinGrow-AI no respondio.");

    private readonly IGmailMailboxClient _mailbox;
    private readonly IGmailOAuthClient _oauth;
    private readonly IAiService _ai;
    private readonly IEmployeeRepository _employees;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<GmailSynchronizer> _logger;

    public GmailSynchronizer(
        IGmailMailboxClient mailbox,
        IGmailOAuthClient oauth,
        IAiService ai,
        IEmployeeRepository employees,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<GmailSynchronizer> logger)
    {
        _mailbox = mailbox;
        _oauth = oauth;
        _ai = ai;
        _employees = employees;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<GmailSyncSummary>> SyncAsync(EmployeeIntegration integration, CancellationToken cancellationToken)
    {
        if (integration.Provider != IntegrationProvider.Gmail || integration.Grant is null)
        {
            return Result.Failure<GmailSyncSummary>(NotLinked);
        }

        if (integration.Status == IntegrationStatus.Error)
        {
            return Result.Failure<GmailSyncSummary>(AccessLost);
        }

        var employee = await _employees.GetByIdAsync(integration.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<GmailSyncSummary>(NotLinked);
        }

        var now = _clock.UtcNow;
        var since = (integration.LastSyncedAt ?? now.Subtract(InitialLookback)).Subtract(Overlap);
        var search = await SearchReauthorizingOnceAsync(integration, GmailBillSearch.Query(since), now, cancellationToken);

        if (search is null)
        {
            return await LoseAccessAsync(integration, now, cancellationToken);
        }

        var (accessToken, messageIds) = search.Value;

        // Gmail devuelve primero los mas nuevos: se procesan del mas viejo al mas nuevo para que, si la
        // corrida se corta, la siguiente retome desde el ultimo mail leido sin saltear ninguno.
        var pending = messageIds.AsEnumerable().Reverse().ToList();
        var batch = pending.Take(MaxMessagesPerRun).ToList();
        var bills = new List<(GmailMessage Message, ParsedBill Bill, string? Model)>();
        var notBills = 0;
        var processed = 0;
        DateTimeOffset? lastProcessedAt = null;
        var aiFailed = false;

        // De a varios en paralelo porque cada mail espera a FinGrow-AI y la sincronizacion manual la
        // espera el empleado; los resultados se recorren en orden para que la marca de agua siga valiendo.
        foreach (var chunk in batch.Chunk(ParallelReads))
        {
            var results = await Task.WhenAll(chunk.Select(messageId =>
                ReadAndParseAsync(accessToken, messageId, employee.PreferredCurrency, cancellationToken)));

            foreach (var (message, parsed) in results)
            {
                if (parsed is null)
                {
                    aiFailed = true;
                    break;
                }

                processed++;
                lastProcessedAt = message.ReceivedAt;

                if (IsImportable(parsed))
                {
                    bills.Add((message, parsed.Bill!, parsed.Model));
                }
                else
                {
                    notBills++;
                }
            }

            if (aiFailed)
            {
                break;
            }
        }

        var imported = await ProposeAsync(integration.EmployeeId, bills, now, cancellationToken);
        var finishedAll = !aiFailed && processed == pending.Count;
        var watermark = finishedAll ? now : lastProcessedAt;

        if (watermark is { } syncedUpTo)
        {
            integration.MarkSynced(syncedUpTo);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = new GmailSyncSummary(imported, bills.Count - imported, notBills, pending.Count - processed, now);
        LogSynced(_logger, integration.EmployeeId, summary.Imported, summary.AlreadyKnown, summary.NotBills, summary.Remaining);

        return aiFailed ? Result.Failure<GmailSyncSummary>(AiUnavailable) : Result.Success(summary);
    }

    private async Task<int> ProposeAsync(
        Guid employeeId,
        List<(GmailMessage Message, ParsedBill Bill, string? Model)> bills,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var candidates = bills
            .Select(bill => (bill.Message, bill.Bill, bill.Model, Reference: GmailBillMapper.ExternalReference(bill.Bill, bill.Message.ReceivedAt)))
            .DistinctBy(candidate => candidate.Reference)
            .ToList();

        if (candidates.Count == 0)
        {
            return 0;
        }

        var known = await _transactions.ListExistingExternalReferencesAsync(
            employeeId,
            TransactionSource.Gmail,
            candidates.Select(candidate => candidate.Reference).ToList(),
            cancellationToken);

        var fresh = candidates.Where(candidate => !known.Contains(candidate.Reference)).ToList();

        foreach (var candidate in fresh)
        {
            _transactions.Add(GmailBillMapper.ToPendingExpense(candidate.Bill, candidate.Message, employeeId, candidate.Model, now));
        }

        return fresh.Count;
    }

    // Solo se importan facturas de servicios. Un viaje (Cabify, Uber) tambien trae un total y el prompt ya
    // lo excluye, pero si el modelo igual lo toma como factura lo delata la categoria.
    private static bool IsImportable(ParsedBillEmail parsed) =>
        parsed.Outcome == BillParsingOutcome.Bill
        && parsed.Bill is not null
        && parsed.Bill.Category != ExpenseCategory.Transporte;

    private async Task<(GmailMessage Message, ParsedBillEmail? Parsed)> ReadAndParseAsync(
        string accessToken,
        string messageId,
        Currency currency,
        CancellationToken cancellationToken)
    {
        var message = await _mailbox.GetMessageAsync(accessToken, messageId, cancellationToken);

        return (message, await TryParseAsync(message, currency, cancellationToken));
    }

    private async Task<ParsedBillEmail?> TryParseAsync(GmailMessage message, Currency currency, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Body))
        {
            return ParsedBillEmail.Of(BillParsingOutcome.NotABill);
        }

        var email = new BillEmail(
            Truncate(message.Subject, MaxSubjectLength),
            message.SenderDomain,
            Truncate(message.Body, MaxBodyLength));

        try
        {
            return await _ai.ParseBillEmailAsync(email, currency, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAiFailed(_logger, exception, message.Id);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogAiFailed(_logger, exception, message.Id);
        }

        return null;
    }

    private async Task<(string AccessToken, List<string> MessageIds)?> SearchReauthorizingOnceAsync(
        EmployeeIntegration integration,
        string query,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessToken = await FreshAccessTokenAsync(integration, force: false, now, cancellationToken);

        if (accessToken is null)
        {
            return null;
        }

        try
        {
            return (accessToken, await SearchAsync(accessToken, query, cancellationToken));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            LogUnauthorized(_logger, integration.EmployeeId);
        }

        accessToken = await FreshAccessTokenAsync(integration, force: true, now, cancellationToken);

        if (accessToken is null)
        {
            return null;
        }

        try
        {
            return (accessToken, await SearchAsync(accessToken, query, cancellationToken));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }
    }

    private async Task<List<string>> SearchAsync(string accessToken, string query, CancellationToken cancellationToken)
    {
        var ids = new List<string>();
        string? pageToken = null;

        do
        {
            var page = await _mailbox.SearchAsync(accessToken, query, pageToken, cancellationToken);

            ids.AddRange(page.MessageIds);
            pageToken = page.NextPageToken;
        }
        while (pageToken is not null && ids.Count < MaxMessagesListed);

        return ids.Distinct(StringComparer.Ordinal).Take(MaxMessagesListed).ToList();
    }

    private async Task<string?> FreshAccessTokenAsync(
        EmployeeIntegration integration,
        bool force,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var grant = integration.Grant!;

        if (!force && !grant.ExpiresWithin(RefreshWindow, now))
        {
            return grant.AccessToken;
        }

        if (!grant.CanRefresh)
        {
            return null;
        }

        GmailTokens refreshed;

        try
        {
            refreshed = await _oauth.RefreshAsync(grant.RefreshToken!, cancellationToken);
        }
        catch (HttpRequestException exception) when (IsRevoked(exception))
        {
            LogRevoked(_logger, integration.EmployeeId);

            return null;
        }

        if (!refreshed.CanReadMail)
        {
            return null;
        }

        // Al renovar, Google no vuelve a mandar el refresh token: el que ya tenemos sigue valiendo.
        var renewed = OAuthGrant.From(
            refreshed.AccessToken,
            refreshed.RefreshToken ?? grant.RefreshToken,
            now.Add(refreshed.ExpiresIn));

        integration.Authorize(renewed, now);

        return renewed.AccessToken;
    }

    // invalid_grant es la respuesta de Google cuando el empleado quito el permiso o el refresh token
    // vencio. Cualquier otro error (Google caido, credenciales de FinGrow mal configuradas) no es culpa
    // del empleado y no tiene que marcarle la integracion con error.
    private static bool IsRevoked(HttpRequestException exception) =>
        exception.StatusCode == HttpStatusCode.BadRequest
        && exception.Message.Contains("invalid_grant", StringComparison.Ordinal);

    private async Task<Result<GmailSyncSummary>> LoseAccessAsync(
        EmployeeIntegration integration,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        integration.MarkFailed(AccessLostReason, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogAccessLost(_logger, integration.EmployeeId);

        return Result.Failure<GmailSyncSummary>(AccessLost);
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length > maxLength ? text[..maxLength] : text;

    [LoggerMessage(Level = LogLevel.Information, Message = "Gmail sincronizado para el empleado {EmployeeId}: {Imported} facturas nuevas, {AlreadyKnown} ya cargadas, {NotBills} mails que no eran facturas, {Remaining} pendientes para la proxima corrida.")]
    private static partial void LogSynced(ILogger logger, Guid employeeId, int imported, int alreadyKnown, int notBills, int remaining);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gmail rechazo el token del empleado {EmployeeId}; se renueva y se reintenta una vez.")]
    private static partial void LogUnauthorized(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google revoco el acceso a Gmail del empleado {EmployeeId}.")]
    private static partial void LogRevoked(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Se perdio el acceso a Gmail del empleado {EmployeeId}: la integracion queda con error.")]
    private static partial void LogAccessLost(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "FinGrow-AI no pudo interpretar el mail {MessageId}; la sincronizacion sigue desde ahi en la proxima corrida.")]
    private static partial void LogAiFailed(ILogger logger, Exception exception, string messageId);
}
