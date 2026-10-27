namespace FinGrow.Infrastructure.Mailing;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Options;

internal sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;

    public ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var (to, subject) = Route(message);
        var request = new SendEmailRequest(
            _options.From,
            new[] { to },
            subject,
            message.HtmlBody,
            message.TextBody,
            message.Attachment is { } attachment
                ? new[] { new SendEmailAttachment(attachment.FileName, attachment.Base64Content) }
                : null);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("emails", request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new EmailDeliveryException("No se pudo conectar con Resend.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmailDeliveryException("Resend no respondio a tiempo.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);

                throw new EmailDeliveryException($"Resend rechazo el correo ({(int)response.StatusCode}): {error}");
            }

            var sent = await response.Content.ReadFromJsonAsync<SendEmailResponse>(cancellationToken);

            return sent?.Id ?? throw new EmailDeliveryException("Resend acepto el correo pero no devolvio su id.");
        }
    }

    private (string To, string Subject) Route(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(_options.RedirectAllTo))
        {
            return (message.To.Value, message.Subject);
        }

        return (_options.RedirectAllTo, $"[Para {message.To.Value}] {message.Subject}");
    }

    private sealed record SendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<string> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("attachments"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<SendEmailAttachment>? Attachments);

    private sealed record SendEmailAttachment(
        [property: JsonPropertyName("filename")] string FileName,
        [property: JsonPropertyName("content")] string Content);

    private sealed record SendEmailResponse([property: JsonPropertyName("id")] string? Id);
}
