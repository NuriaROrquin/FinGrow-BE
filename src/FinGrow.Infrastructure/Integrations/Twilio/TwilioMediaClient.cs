namespace FinGrow.Infrastructure.Integrations.Twilio;

using FinGrow.Application.Interfaces;

internal sealed class TwilioMediaClient : ITwilioMediaClient
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly HttpClient _httpClient;

    public TwilioMediaClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<TwilioMedia> DownloadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? DefaultContentType;

        return new TwilioMedia(content, contentType);
    }
}
