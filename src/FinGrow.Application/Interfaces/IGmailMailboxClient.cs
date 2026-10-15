namespace FinGrow.Application.Interfaces;

public sealed record GmailMessagePage(IReadOnlyList<string> MessageIds, string? NextPageToken);

/// <summary>
/// Un mail ya convertido a texto plano. <c>SenderDomain</c> es solo el dominio del remitente.
/// </summary>
public sealed record GmailMessage(
    string Id,
    string Subject,
    string SenderDomain,
    DateTimeOffset ReceivedAt,
    string Body);

public interface IGmailMailboxClient
{
    /// <summary>Busca con la sintaxis del buscador de Gmail; devuelve primero los mas nuevos.</summary>
    Task<GmailMessagePage> SearchAsync(
        string accessToken,
        string query,
        string? pageToken,
        CancellationToken cancellationToken = default);

    Task<GmailMessage> GetMessageAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken = default);
}
