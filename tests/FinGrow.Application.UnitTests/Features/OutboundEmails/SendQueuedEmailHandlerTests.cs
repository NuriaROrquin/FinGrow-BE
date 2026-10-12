namespace FinGrow.Application.UnitTests.Features.OutboundEmails;

using FinGrow.Application.Events;
using FinGrow.Application.Features.OutboundEmails;
using FinGrow.Application.Features.OutboundEmails.SendQueuedEmail;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class SendQueuedEmailHandlerTests
{
    private static readonly DateTimeOffset QueuedAt = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = QueuedAt.AddSeconds(1);

    private readonly FakeOutboundEmailRepository _emails = new();
    private readonly FakeEmailSender _sender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task A_queued_email_is_sent_and_recorded_as_sent()
    {
        var email = Queue();

        await Handle(email.Id);

        var message = _sender.Sent.ShouldHaveSingleItem();
        message.To.Value.ShouldBe("ana@acme.com");
        message.Subject.ShouldBe("Tu acceso a FinGrow");
        message.HtmlBody.ShouldBe("<p>Hola Ana</p>");
        message.TextBody.ShouldBe("Hola Ana");
        email.Status.ShouldBe(OutboundEmailStatus.Sent);
        email.ProviderMessageId.ShouldBe("re-1");
        email.SentAt.ShouldBe(Now);
        email.Attempts.ShouldBe(1);
        email.HtmlBody.ShouldBeNull();
        email.TextBody.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_delivery_failure_is_recorded_for_retry_instead_of_escaping()
    {
        var email = Queue();
        _sender.Failure = new EmailDeliveryException("Resend rechazo el correo (503): caido");

        await Handle(email.Id);

        email.Status.ShouldBe(OutboundEmailStatus.Failed);
        email.Attempts.ShouldBe(1);
        email.LastError.ShouldBe("Resend rechazo el correo (503): caido");
        email.NextAttemptAt.ShouldBe(Now.AddMinutes(5));
        email.TextBody.ShouldBe("Hola Ana");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_email_that_is_no_longer_pending_is_not_sent_again()
    {
        var email = Queue();
        email.MarkSent("re-previo", QueuedAt);

        await Handle(email.Id);

        _sender.Sent.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_missing_email_is_ignored()
    {
        await Handle(Guid.CreateVersion7());

        _sender.Sent.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private OutboundEmail Queue()
    {
        var email = OutboundEmail.Queue(Email.From("ana@acme.com"), "Tu acceso a FinGrow", "<p>Hola Ana</p>", "Hola Ana", QueuedAt);
        _emails.Add(email);

        return email;
    }

    private Task Handle(Guid outboundEmailId)
    {
        var dispatcher = new OutboundEmailDispatcher(
            _sender,
            _unitOfWork,
            new FakeDateTimeProvider(Now),
            NullLogger<OutboundEmailDispatcher>.Instance);

        return new SendQueuedEmailHandler(_emails, dispatcher).Handle(
            new DomainEventEnvelope<OutboundEmailQueued>(new OutboundEmailQueued(outboundEmailId, QueuedAt)),
            CancellationToken.None);
    }
}
