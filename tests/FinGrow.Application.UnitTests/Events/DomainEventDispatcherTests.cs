namespace FinGrow.Application.UnitTests.Events;

using FinGrow.Application.Events;
using FinGrow.Domain.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed record SampleHappened(string Label, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record SampleExploded(DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record SampleIgnored(DateTimeOffset OccurredAt) : IDomainEvent;

public sealed class ReactionLog
{
    public List<(string Label, Guid ScopeId)> Entries { get; } = new();
}

public sealed class ScopeMarker
{
    public Guid Id { get; } = Guid.CreateVersion7();
}

public sealed class RecordSampleHappened(ReactionLog log, ScopeMarker scope)
    : INotificationHandler<DomainEventEnvelope<SampleHappened>>
{
    public Task Handle(DomainEventEnvelope<SampleHappened> notification, CancellationToken cancellationToken)
    {
        log.Entries.Add((notification.DomainEvent.Label, scope.Id));
        return Task.CompletedTask;
    }
}

public sealed class ExplodeOnSampleExploded : INotificationHandler<DomainEventEnvelope<SampleExploded>>
{
    public Task Handle(DomainEventEnvelope<SampleExploded> notification, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("la reaccion se rompio");
}

public class DomainEventDispatcherTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Each_event_reaches_its_handler_in_a_scope_of_its_own()
    {
        await using var provider = BuildProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(new IDomainEvent[]
        {
            new SampleHappened("primero", Now),
            new SampleHappened("segundo", Now),
        });

        var log = provider.GetRequiredService<ReactionLog>();
        log.Entries.Count.ShouldBe(2);
        log.Entries[0].Label.ShouldBe("primero");
        log.Entries[1].Label.ShouldBe("segundo");
        log.Entries[0].ScopeId.ShouldNotBe(log.Entries[1].ScopeId);
    }

    [Fact]
    public async Task A_failing_reaction_neither_reaches_the_caller_nor_stops_the_next_events()
    {
        await using var provider = BuildProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        await Should.NotThrowAsync(() => dispatcher.DispatchAsync(new IDomainEvent[]
        {
            new SampleExploded(Now),
            new SampleHappened("despues del error", Now),
        }));

        provider.GetRequiredService<ReactionLog>().Entries.ShouldHaveSingleItem().Label.ShouldBe("despues del error");
    }

    [Fact]
    public async Task An_event_nobody_listens_to_is_simply_ignored()
    {
        await using var provider = BuildProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        await Should.NotThrowAsync(() => dispatcher.DispatchAsync(new IDomainEvent[] { new SampleIgnored(Now) }));

        provider.GetRequiredService<ReactionLog>().Entries.ShouldBeEmpty();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DomainEventDispatcherTests).Assembly));
        services.AddSingleton<ReactionLog>();
        services.AddScoped<ScopeMarker>();

        return services.BuildServiceProvider();
    }
}
