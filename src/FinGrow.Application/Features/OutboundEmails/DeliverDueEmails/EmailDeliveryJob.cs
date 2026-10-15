namespace FinGrow.Application.Features.OutboundEmails.DeliverDueEmails;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class EmailDeliveryJob : IScheduledJob
{
    public const string JobName = "email-delivery";
    public const int BatchSize = 50;

    private readonly IOutboundEmailRepository _emails;
    private readonly IDateTimeProvider _clock;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EmailDeliveryJob> _logger;

    public EmailDeliveryJob(
        IOutboundEmailRepository emails,
        IDateTimeProvider clock,
        IServiceScopeFactory scopes,
        ILogger<EmailDeliveryJob> logger)
    {
        _emails = emails;
        _clock = clock;
        _scopes = scopes;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description => string.Create(
        CultureInfo.InvariantCulture,
        $"Reintenta los correos que no se pudieron enviar en el momento (por ejemplo, porque Resend no respondia) y abandona los que fallan {OutboundEmail.MaxAttempts} veces.");

    public string Schedule => "*/5 * * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var due = await _emails.ListDueIdsAsync(_clock.UtcNow, BatchSize, cancellationToken);

        var sent = 0;
        var failures = new List<string>();

        foreach (var emailId in due)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var emails = scope.ServiceProvider.GetRequiredService<IOutboundEmailRepository>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboundEmailDispatcher>();

                if (await emails.GetByIdAsync(emailId, cancellationToken) is not { IsAwaitingDelivery: true } email)
                {
                    continue;
                }

                await dispatcher.DispatchAsync(email, cancellationToken);

                if (email.Status == OutboundEmailStatus.Sent)
                {
                    sent++;
                }
                else
                {
                    failures.Add($"Correo {emailId} ({email.Status}, intento {email.Attempts}): {email.LastError}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailed(_logger, exception, emailId);
                failures.Add($"Correo {emailId}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{due.Count} correo(s) para enviar: {sent} enviado(s), {failures.Count} sin enviar.");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo el reintento del correo {OutboundEmailId}.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid outboundEmailId);
}
