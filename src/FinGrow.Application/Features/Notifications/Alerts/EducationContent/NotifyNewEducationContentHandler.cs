namespace FinGrow.Application.Features.Notifications.Alerts.EducationContent;

using FinGrow.Application.Events;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Un curso o un articulo se publica una sola vez y le llega a todos los empleados con acceso. Cada uno se
/// avisa en su propio scope: si a uno le falla el guardado, el DbContext queda roto y arrastraria a los que siguen.
/// </summary>
internal sealed partial class NotifyNewEducationContentHandler
    : INotificationHandler<DomainEventEnvelope<EducationContentPublished>>
{
    internal const string Title = "Contenido educativo nuevo";

    // Un contenido se publica una sola vez; la ventana solo cubre un evento reprocesado.
    internal static readonly TimeSpan DeduplicationWindow = TimeSpan.FromDays(366 * 10);

    private readonly INotificationAudienceRepository _audience;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotifyNewEducationContentHandler> _logger;

    public NotifyNewEducationContentHandler(
        INotificationAudienceRepository audience,
        IServiceScopeFactory scopes,
        ILogger<NotifyNewEducationContentHandler> logger)
    {
        _audience = audience;
        _scopes = scopes;
        _logger = logger;
    }

    public async Task Handle(DomainEventEnvelope<EducationContentPublished> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var published = notification.DomainEvent;
        var kind = published.Kind == EducationContentKind.Course ? "curso" : "artículo";

        foreach (var employeeId in await _audience.ListActiveEmployeeIdsAsync(cancellationToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var notifier = scope.ServiceProvider.GetRequiredService<Notifier>();

                await notifier.NotifyAsync(
                    new NotificationRequest(
                        NotificationRecipient.Employee(employeeId),
                        NotificationType.NewEducationContent,
                        Title,
                        $"Ya podés ver el {kind} \"{published.Title}\" en Educación.",
                        $"education-content:{published.Kind}:{published.ContentId}",
                        DeduplicationWindow),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogEmployeeFailed(_logger, exception, employeeId, published.ContentId);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo avisar al empleado {EmployeeId} del contenido nuevo {ContentId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId, Guid contentId);
}
