namespace FinGrow.Infrastructure.Ai;

using FinGrow.Application.Interfaces;

internal sealed class AiService : IAiService
{
    private readonly HttpClient _httpClient;

    public AiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
