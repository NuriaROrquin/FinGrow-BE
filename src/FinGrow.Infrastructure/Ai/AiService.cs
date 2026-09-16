namespace FinGrow.Infrastructure.Ai;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;

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

    public async Task<IReadOnlyList<CategorizedExpense>> CategorizeExpensesAsync(
        IReadOnlyList<ExpenseToCategorize> expenses,
        CancellationToken cancellationToken = default)
    {
        var request = new CategorizeRequest(expenses
            .Select(expense => new TransactionInput(
                expense.Id,
                expense.Description,
                expense.Amount,
                expense.Currency.ToString(),
                expense.OccurredOn))
            .ToList());

        using var response = await _httpClient.PostAsJsonAsync("/api/v1/categorization/transactions", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CategorizeResponse>(cancellationToken)
            ?? throw new HttpRequestException("FinGrow-AI devolvio una respuesta vacia al categorizar.");

        return payload.Results
            .Where(result => ExpenseCategoryExtensions.TryFromWireValue(result.Category, out _))
            .Select(result => new CategorizedExpense(
                result.Id,
                ExpenseCategoryExtensions.FromWireValue(result.Category),
                result.Confidence))
            .ToList();
    }

    private sealed record CategorizeRequest([property: JsonPropertyName("transactions")] List<TransactionInput> Transactions);

    private sealed record TransactionInput(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("occurred_on")] DateOnly OccurredOn);

    private sealed record CategorizeResponse(
        [property: JsonPropertyName("results")] List<CategorizedTransaction> Results,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record CategorizedTransaction(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("confidence")] double Confidence);
}
