namespace FinGrow.Infrastructure.Ai;

using System.Collections.Frozen;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;

internal sealed class AiService : IAiService
{
    private static readonly FrozenDictionary<string, MessageParsingOutcome> Outcomes =
        new Dictionary<string, MessageParsingOutcome>(StringComparer.Ordinal)
        {
            ["parsed"] = MessageParsingOutcome.Parsed,
            ["missing_amount"] = MessageParsingOutcome.MissingAmount,
            ["not_a_transaction"] = MessageParsingOutcome.NotATransaction,
            ["multiple_transactions"] = MessageParsingOutcome.MultipleTransactions
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TransactionType> Types =
        new Dictionary<string, TransactionType>(StringComparer.Ordinal)
        {
            ["expense"] = TransactionType.Expense,
            ["income"] = TransactionType.Income
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, PaymentMethod> PaymentMethods =
        new Dictionary<string, PaymentMethod>(StringComparer.Ordinal)
        {
            ["cash"] = PaymentMethod.Cash,
            ["credit_card"] = PaymentMethod.CreditCard,
            ["debit_card"] = PaymentMethod.DebitCard,
            ["bank_transfer"] = PaymentMethod.BankTransfer,
            ["digital_wallet"] = PaymentMethod.DigitalWallet
        }.ToFrozenDictionary(StringComparer.Ordinal);

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
            .Where(result => ConfidenceScale.IsValid(result.Confidence))
            .Select(result => new CategorizedExpense(
                result.Id,
                ExpenseCategoryExtensions.FromWireValue(result.Category),
                result.Confidence,
                payload.Model))
            .ToList();
    }

    public async Task<ParsedMessage> ParseTransactionMessageAsync(
        string text,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default)
    {
        var request = new ParseMessageRequest(text, defaultCurrency.ToString());

        using var response = await _httpClient.PostAsJsonAsync("/api/v1/message-parsing/transactions", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        ParseMessageResponse? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<ParseMessageResponse>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("FinGrow-AI devolvio un JSON invalido al interpretar el mensaje.", exception);
        }

        if (payload is null || !Outcomes.TryGetValue(payload.Status ?? string.Empty, out var outcome))
        {
            throw new HttpRequestException("FinGrow-AI devolvio un status desconocido al interpretar el mensaje.");
        }

        if (outcome != MessageParsingOutcome.Parsed)
        {
            return ParsedMessage.Of(outcome, payload.Model);
        }

        return new ParsedMessage(outcome, ToParsedTransaction(payload.Transaction), payload.Model);
    }

    private static ParsedTransaction ToParsedTransaction(ParsedTransactionPayload? transaction)
    {
        if (transaction is null
            || !Types.TryGetValue(transaction.Type ?? string.Empty, out var type)
            || !Enum.TryParse<Currency>(transaction.Currency, ignoreCase: false, out var currency)
            || !Enum.IsDefined(currency)
            || transaction.Amount <= 0
            || string.IsNullOrWhiteSpace(transaction.Description)
            || !ConfidenceScale.IsValid(transaction.Confidence))
        {
            throw new HttpRequestException("FinGrow-AI devolvio un movimiento incompleto.");
        }

        ExpenseCategory? expenseCategory = null;
        IncomeCategory? incomeCategory = null;

        if (type == TransactionType.Expense && ExpenseCategoryExtensions.TryFromWireValue(transaction.ExpenseCategory, out var expense))
        {
            expenseCategory = expense;
        }
        else if (type == TransactionType.Income && IncomeCategoryExtensions.TryFromWireValue(transaction.IncomeCategory, out var income))
        {
            incomeCategory = income;
        }
        else
        {
            throw new HttpRequestException("FinGrow-AI devolvio una categoria que no corresponde al tipo de movimiento.");
        }

        PaymentMethod? paymentMethod = transaction.PaymentMethod is not null
            && PaymentMethods.TryGetValue(transaction.PaymentMethod, out var method)
                ? method
                : null;

        return new ParsedTransaction(
            type,
            transaction.Amount,
            currency,
            expenseCategory,
            incomeCategory,
            transaction.Description.Trim(),
            paymentMethod,
            transaction.Confidence);
    }

    private sealed record ParseMessageRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("default_currency")] string DefaultCurrency);

    private sealed record ParseMessageResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("transaction")] ParsedTransactionPayload? Transaction,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record ParsedTransactionPayload(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("amount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("expense_category")] string? ExpenseCategory,
        [property: JsonPropertyName("income_category")] string? IncomeCategory,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("payment_method")] string? PaymentMethod,
        [property: JsonPropertyName("confidence")] double Confidence);

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
