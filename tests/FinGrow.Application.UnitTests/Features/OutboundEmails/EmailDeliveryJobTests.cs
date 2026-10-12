namespace FinGrow.Application.UnitTests.Features.OutboundEmails;

using FinGrow.Application.Features.OutboundEmails.DeliverDueEmails;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class EmailDeliveryJobTests
{
    private static readonly DateTimeOffset QueuedAt = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeOutboundEmailRepository _emails = new();
    private readonly FakeEmailSender _sender = new();
    private readonly FakeDateTimeProvider _clock = new(QueuedAt.AddMinutes(10));
    private readonly ServiceProvider _provider;

    public EmailDeliveryJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddSingleton<IOutboundEmailRepository>(_emails);
        services.AddSingleton<IEmailSender>(_sender);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(_clock);

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task A_failed_email_whose_retry_is_due_is_sent()
    {
        var email = Queue("ana@acme.com");
        email.MarkFailed("Resend no respondio a tiempo.", QueuedAt);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("1 correo(s) para enviar: 1 enviado(s), 0 sin enviar.");
        email.Status.ShouldBe(OutboundEmailStatus.Sent);
        email.Attempts.ShouldBe(2);
        _sender.Sent.ShouldHaveSingleItem().To.Value.ShouldBe("ana@acme.com");
    }

    [Fact]
    public async Task A_pending_email_that_its_first_attempt_left_behind_is_sent()
    {
        var email = Queue("ana@acme.com");

        await Job().ExecuteAsync(CancellationToken.None);

        email.Status.ShouldBe(OutboundEmailStatus.Sent);
    }

    [Fact]
    public async Task Emails_that_are_not_due_yet_are_left_alone()
    {
        _clock.UtcNow = QueuedAt.AddSeconds(30);
        var justQueued = Queue("ana@acme.com");
        var waitingRetry = Queue("bruno@acme.com");
        waitingRetry.MarkFailed("caido", QueuedAt.AddSeconds(10));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary.ShouldBe("0 correo(s) para enviar: 0 enviado(s), 0 sin enviar.");
        justQueued.Status.ShouldBe(OutboundEmailStatus.Pending);
        waitingRetry.Status.ShouldBe(OutboundEmailStatus.Failed);
        _sender.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sent_and_abandoned_emails_are_never_retried()
    {
        Queue("ana@acme.com").MarkSent("re-1", QueuedAt);
        var abandoned = Queue("bruno@acme.com");
        for (var attempt = 0; attempt < OutboundEmail.MaxAttempts; attempt++)
        {
            abandoned.MarkFailed("caido", QueuedAt);
        }

        await Job().ExecuteAsync(CancellationToken.None);

        _sender.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_retry_that_fails_again_is_rescheduled_and_reported()
    {
        var email = Queue("ana@acme.com");
        email.MarkFailed("caido", QueuedAt);
        _sender.Failure = new EmailDeliveryException("Resend rechazo el correo (503): caido");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Summary.ShouldBe("1 correo(s) para enviar: 0 enviado(s), 1 sin enviar.");
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(email.Id.ToString());
        result.Error.ShouldContain("Resend rechazo el correo (503): caido");
        email.Status.ShouldBe(OutboundEmailStatus.Failed);
        email.Attempts.ShouldBe(2);
        email.NextAttemptAt.ShouldBe(_clock.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task The_last_failed_attempt_abandons_the_email()
    {
        var email = Queue("ana@acme.com");
        for (var attempt = 1; attempt < OutboundEmail.MaxAttempts; attempt++)
        {
            email.MarkFailed("caido", QueuedAt);
        }

        _clock.UtcNow = email.NextAttemptAt!.Value;
        _sender.Failure = new EmailDeliveryException("caido otra vez");

        await Job().ExecuteAsync(CancellationToken.None);

        email.Status.ShouldBe(OutboundEmailStatus.Abandoned);
        email.TextBody.ShouldBeNull();
    }

    private OutboundEmail Queue(string recipient)
    {
        var email = OutboundEmail.Queue(Email.From(recipient), "Tu acceso a FinGrow", "<p>Hola</p>", "Hola", QueuedAt);
        _emails.Add(email);

        return email;
    }

    private EmailDeliveryJob Job() => new(
        _emails,
        _clock,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<EmailDeliveryJob>.Instance);
}
