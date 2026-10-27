namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.ValueObjects;

public sealed record EmailMessage(
    Email To,
    string Subject,
    string HtmlBody,
    string TextBody,
    EmailAttachment? Attachment = null);

public interface IEmailSender
{
    Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException()
    {
    }

    public EmailDeliveryException(string message) : base(message)
    {
    }

    public EmailDeliveryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
