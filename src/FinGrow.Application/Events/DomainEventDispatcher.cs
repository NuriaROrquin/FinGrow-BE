namespace FinGrow.Application.Events;

using FinGrow.Domain.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceScopeFactory scopes, ILogger<DomainEventDispatcher> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                await publisher.Publish(Wrap(domainEvent), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogReactionFailed(_logger, exception, domainEvent.GetType().Name);
            }
        }
    }

    private static INotification Wrap(IDomainEvent domainEvent) =>
        (INotification)Activator.CreateInstance(
            typeof(DomainEventEnvelope<>).MakeGenericType(domainEvent.GetType()),
            domainEvent)!;

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo una reaccion al evento de dominio {EventName}; el cambio que lo origino ya habia quedado guardado.")]
    private static partial void LogReactionFailed(ILogger logger, Exception exception, string eventName);
}
