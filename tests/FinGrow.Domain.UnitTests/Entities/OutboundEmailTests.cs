namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

public class OutboundEmailTests
{
    private static readonly DateTimeOffset QueuedAt = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_queued_email_is_pending_and_due_right_away()
    {
        var email = Queue();

        email.Status.ShouldBe(OutboundEmailStatus.Pending);
        email.IsAwaitingDelivery.ShouldBeTrue();
        email.Recipient.Value.ShouldBe("ana@acme.com");
        email.Subject.ShouldBe("Tu acceso a FinGrow");
        email.Attempts.ShouldBe(0);
        email.NextAttemptAt.ShouldBe(QueuedAt);
        email.CreatedAt.ShouldBe(QueuedAt);
    }

    [Fact]
    public void Queuing_raises_the_event_that_triggers_the_first_attempt()
    {
        var email = Queue();

        var raised = email.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<OutboundEmailQueued>();
        raised.OutboundEmailId.ShouldBe(email.Id);
        raised.OccurredAt.ShouldBe(QueuedAt);
    }

    [Fact]
    public void Sending_records_the_provider_id_and_drops_the_body()
    {
        var email = Queue();

        email.MarkSent(" re-123 ", QueuedAt.AddSeconds(1));

        email.Status.ShouldBe(OutboundEmailStatus.Sent);
        email.IsAwaitingDelivery.ShouldBeFalse();
        email.ProviderMessageId.ShouldBe("re-123");
        email.SentAt.ShouldBe(QueuedAt.AddSeconds(1));
        email.Attempts.ShouldBe(1);
        email.NextAttemptAt.ShouldBeNull();
        email.HtmlBody.ShouldBeNull();
        email.TextBody.ShouldBeNull();
    }

    [Fact]
    public void Each_failure_waits_longer_before_the_next_attempt()
    {
        var email = Queue();
        var expectedDelays = new[]
        {
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(4),
        };

        foreach (var delay in expectedDelays)
        {
            email.MarkFailed("Resend no respondio a tiempo.", QueuedAt);

            email.Status.ShouldBe(OutboundEmailStatus.Failed);
            email.NextAttemptAt.ShouldBe(QueuedAt + delay);
            email.HtmlBody.ShouldNotBeNull();
        }

        email.Attempts.ShouldBe(4);
        email.LastError.ShouldBe("Resend no respondio a tiempo.");
    }

    [Fact]
    public void After_the_last_attempt_fails_the_email_is_abandoned_and_its_body_dropped()
    {
        var email = Queue();

        for (var attempt = 1; attempt <= OutboundEmail.MaxAttempts; attempt++)
        {
            email.MarkFailed($"fallo {attempt}", QueuedAt);
        }

        email.Status.ShouldBe(OutboundEmailStatus.Abandoned);
        email.IsAwaitingDelivery.ShouldBeFalse();
        email.Attempts.ShouldBe(OutboundEmail.MaxAttempts);
        email.LastError.ShouldBe($"fallo {OutboundEmail.MaxAttempts}");
        email.NextAttemptAt.ShouldBeNull();
        email.HtmlBody.ShouldBeNull();
        email.TextBody.ShouldBeNull();
    }

    [Fact]
    public void A_failed_email_can_still_be_sent()
    {
        var email = Queue();
        email.MarkFailed("caido", QueuedAt);

        email.MarkSent("re-123", QueuedAt.AddMinutes(5));

        email.Status.ShouldBe(OutboundEmailStatus.Sent);
        email.Attempts.ShouldBe(2);
        email.LastError.ShouldBe("caido");
    }

    [Fact]
    public void A_long_error_is_truncated()
    {
        var email = Queue();

        email.MarkFailed(new string('x', OutboundEmail.MaxErrorLength + 10), QueuedAt);

        email.LastError!.Length.ShouldBe(OutboundEmail.MaxErrorLength);
    }

    [Fact]
    public void An_email_that_was_already_sent_cannot_be_sent_or_failed_again()
    {
        var email = Queue();
        email.MarkSent("re-123", QueuedAt);

        Should.Throw<DomainException>(() => email.MarkSent("re-456", QueuedAt));
        Should.Throw<DomainException>(() => email.MarkFailed("caido", QueuedAt));
    }

    [Fact]
    public void A_failure_without_error_or_a_send_without_provider_id_is_rejected()
    {
        var email = Queue();

        Should.Throw<DomainException>(() => email.MarkFailed("  ", QueuedAt));
        Should.Throw<DomainException>(() => email.MarkSent("  ", QueuedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_email_without_subject_is_rejected(string subject) =>
        Should.Throw<DomainException>(() =>
            OutboundEmail.Queue(Email.From("ana@acme.com"), subject, "<p>Hola</p>", "Hola", QueuedAt));

    [Fact]
    public void An_email_without_body_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            OutboundEmail.Queue(Email.From("ana@acme.com"), "Asunto", " ", "Hola", QueuedAt));
        Should.Throw<DomainException>(() =>
            OutboundEmail.Queue(Email.From("ana@acme.com"), "Asunto", "<p>Hola</p>", "", QueuedAt));
    }

    private static OutboundEmail Queue() =>
        OutboundEmail.Queue(Email.From("ana@acme.com"), " Tu acceso a FinGrow ", "<p>Hola Ana</p>", "Hola Ana", QueuedAt);
}
