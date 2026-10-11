namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

public sealed class OutboundEmail : AggregateRoot
{
    public const int MaxSubjectLength = 300;
    public const int MaxErrorLength = 4000;
    public const int MaxProviderMessageIdLength = 100;

    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4),
    };

    private OutboundEmail()
    {
    }

    private OutboundEmail(Guid id, Email recipient, string subject, string htmlBody, string textBody, DateTimeOffset createdAt)
        : base(id)
    {
        Recipient = recipient;
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        Status = OutboundEmailStatus.Pending;
        NextAttemptAt = createdAt;
        CreatedAt = createdAt;
    }

    public static int MaxAttempts => RetryDelays.Length + 1;

    public Email Recipient { get; private set; } = null!;

    public string Subject { get; private set; } = string.Empty;

    public string? HtmlBody { get; private set; }

    public string? TextBody { get; private set; }

    public OutboundEmailStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public string? LastError { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public bool IsAwaitingDelivery => Status is OutboundEmailStatus.Pending or OutboundEmailStatus.Failed;

    public static OutboundEmail Queue(Email recipient, string subject, string htmlBody, string textBody, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        var email = new OutboundEmail(
            Guid.CreateVersion7(),
            recipient,
            EnsureValidSubject(subject),
            EnsureBody(htmlBody, "HTML"),
            EnsureBody(textBody, "de texto"),
            queuedAt);

        email.Raise(new OutboundEmailQueued(email.Id, queuedAt));

        return email;
    }

    public void MarkSent(string providerMessageId, DateTimeOffset sentAt)
    {
        EnsureAwaitingDelivery();

        var trimmedId = (providerMessageId ?? string.Empty).Trim();

        if (trimmedId.Length is 0 or > MaxProviderMessageIdLength)
        {
            throw new DomainException(
                $"El id que asigna el proveedor es obligatorio y no puede superar los {MaxProviderMessageIdLength} caracteres.");
        }

        Attempts++;
        Status = OutboundEmailStatus.Sent;
        SentAt = sentAt;
        ProviderMessageId = trimmedId;
        StopDelivering();
    }

    public void MarkFailed(string error, DateTimeOffset failedAt)
    {
        EnsureAwaitingDelivery();

        var trimmedError = (error ?? string.Empty).Trim();

        if (trimmedError.Length == 0)
        {
            throw new DomainException("Un envio fallido tiene que registrar el error.");
        }

        Attempts++;
        LastError = trimmedError.Length <= MaxErrorLength ? trimmedError : trimmedError[..MaxErrorLength];

        if (Attempts >= MaxAttempts)
        {
            Status = OutboundEmailStatus.Abandoned;
            StopDelivering();
            return;
        }

        Status = OutboundEmailStatus.Failed;
        NextAttemptAt = failedAt + RetryDelays[Attempts - 1];
    }

    private void StopDelivering()
    {
        NextAttemptAt = null;
        HtmlBody = null;
        TextBody = null;
    }

    private void EnsureAwaitingDelivery()
    {
        if (!IsAwaitingDelivery)
        {
            throw new DomainException("El correo ya no espera ser enviado.");
        }
    }

    private static string EnsureValidSubject(string subject)
    {
        var trimmed = (subject ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El asunto del correo es obligatorio."),
            > MaxSubjectLength => throw new DomainException(
                $"El asunto del correo no puede superar los {MaxSubjectLength} caracteres."),
            _ => trimmed,
        };
    }

    private static string EnsureBody(string body, string kind) =>
        string.IsNullOrWhiteSpace(body)
            ? throw new DomainException($"El cuerpo {kind} del correo es obligatorio.")
            : body;
}
