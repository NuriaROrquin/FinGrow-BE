namespace FinGrow.Infrastructure.Integrations.MercadoPago;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

internal sealed class MercadoPagoPaymentsClient : IMercadoPagoPaymentsClient
{
    internal const int PageSize = 50;
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private readonly HttpClient _httpClient;

    public MercadoPagoPaymentsClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<MercadoPagoPaymentsPage> SearchUpdatedBetweenAsync(
        string accessToken,
        DateTimeOffset from,
        DateTimeOffset to,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["sort"] = "date_last_updated",
            ["criteria"] = "asc",
            ["range"] = "date_last_updated",
            ["begin_date"] = from.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            ["end_date"] = to.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            ["limit"] = PageSize.ToString(CultureInfo.InvariantCulture),
            ["offset"] = offset.ToString(CultureInfo.InvariantCulture),
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("v1/payments/search", query));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken)
            ?? throw new HttpRequestException("Mercado Pago devolvio una respuesta vacia al buscar pagos.");

        var results = payload.Results
            .Select(payment => new MercadoPagoPayment(
                payment.Id,
                payment.Status ?? string.Empty,
                payment.OperationType ?? string.Empty,
                payment.TransactionAmount,
                payment.CurrencyId ?? string.Empty,
                payment.Description,
                payment.PaymentTypeId,
                payment.CollectorId,
                payment.Payer?.Id,
                payment.DateCreated,
                payment.DateApproved))
            .ToList();

        return new MercadoPagoPaymentsPage(results, payload.Paging?.Total ?? results.Count);
    }

    private sealed record SearchResponse(
        [property: JsonPropertyName("paging")] Paging? Paging,
        [property: JsonPropertyName("results")] List<Payment> Results);

    private sealed record Paging([property: JsonPropertyName("total")] int Total);

    private sealed record Payment(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("operation_type")] string? OperationType,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
        [property: JsonPropertyName("currency_id")] string? CurrencyId,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("payment_type_id")] string? PaymentTypeId,
        [property: JsonPropertyName("collector_id"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] long? CollectorId,
        [property: JsonPropertyName("payer")] Payer? Payer,
        [property: JsonPropertyName("date_created")] DateTimeOffset DateCreated,
        [property: JsonPropertyName("date_approved")] DateTimeOffset? DateApproved);

    private sealed record Payer(
        [property: JsonPropertyName("id"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] long? Id);
}
