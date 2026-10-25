namespace FinGrow.Infrastructure.Ai;

using System.Collections.Frozen;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

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

    private static readonly FrozenDictionary<string, BillParsingOutcome> BillOutcomes =
        new Dictionary<string, BillParsingOutcome>(StringComparer.Ordinal)
        {
            ["bill"] = BillParsingOutcome.Bill,
            ["missing_amount"] = BillParsingOutcome.MissingAmount,
            ["not_a_bill"] = BillParsingOutcome.NotABill
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

    private static readonly FrozenDictionary<string, AudioTranscriptionOutcome> TranscriptionErrors =
        new Dictionary<string, AudioTranscriptionOutcome>(StringComparer.Ordinal)
        {
            ["audio_not_understood"] = AudioTranscriptionOutcome.NotUnderstood,
            ["unsupported_audio"] = AudioTranscriptionOutcome.UnsupportedFormat,
            ["audio_too_large"] = AudioTranscriptionOutcome.TooLarge
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, ReceiptScanOutcome> ReceiptScanStatuses =
        new Dictionary<string, ReceiptScanOutcome>(StringComparer.Ordinal)
        {
            ["parsed"] = ReceiptScanOutcome.Scanned,
            ["missing_amount"] = ReceiptScanOutcome.MissingAmount,
            ["unreadable"] = ReceiptScanOutcome.Unreadable
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, ReceiptScanOutcome> ReceiptScanErrors =
        new Dictionary<string, ReceiptScanOutcome>(StringComparer.Ordinal)
        {
            ["document_not_readable"] = ReceiptScanOutcome.Unreadable,
            ["unsupported_document"] = ReceiptScanOutcome.UnsupportedDocument,
            ["document_too_large"] = ReceiptScanOutcome.TooLarge
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, RecommendationKind> RecommendationKinds =
        new Dictionary<string, RecommendationKind>(StringComparer.Ordinal)
        {
            ["positive"] = RecommendationKind.Positive,
            ["warning"] = RecommendationKind.Warning,
            ["tip"] = RecommendationKind.Tip
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, RecommendationAction> RecommendationActions =
        new Dictionary<string, RecommendationAction>(StringComparer.Ordinal)
        {
            ["none"] = RecommendationAction.None,
            ["budgets"] = RecommendationAction.Budgets,
            ["goals"] = RecommendationAction.Goals,
            ["transactions"] = RecommendationAction.Transactions,
            ["education"] = RecommendationAction.Education
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<CompanyConcern, string> CompanyConcernWireValues =
        new Dictionary<CompanyConcern, string>
        {
            [CompanyConcern.LowSavings] = "low_savings",
            [CompanyConcern.ThinCushion] = "thin_cushion",
            [CompanyConcern.BudgetOverruns] = "budget_overruns",
            [CompanyConcern.MissedGoalContributions] = "missed_goal_contributions"
        }.ToFrozenDictionary();

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache? _recommendationsCache;
    private readonly TimeSpan _recommendationsCacheDuration;

    public AiService(
        HttpClient httpClient,
        IMemoryCache? recommendationsCache = null,
        IOptions<AiServiceOptions>? options = null)
    {
        _httpClient = httpClient;
        _recommendationsCache = recommendationsCache;
        _recommendationsCacheDuration = TimeSpan.FromHours(options?.Value.RecommendationsCacheHours ?? 0);
    }

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
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var request = new ParseMessageRequest(text, defaultCurrency.ToString(), today);

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

    public async Task<ParsedBillEmail> ParseBillEmailAsync(
        BillEmail email,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var request = new ParseBillRequest(email.Subject, email.Sender, email.Body, defaultCurrency.ToString());

        using var response = await _httpClient.PostAsJsonAsync("/api/v1/email-parsing/bills", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        ParseBillResponse? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<ParseBillResponse>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("FinGrow-AI devolvio un JSON invalido al interpretar el mail.", exception);
        }

        if (payload is null || !BillOutcomes.TryGetValue(payload.Status ?? string.Empty, out var outcome))
        {
            throw new HttpRequestException("FinGrow-AI devolvio un status desconocido al interpretar el mail.");
        }

        return outcome == BillParsingOutcome.Bill
            ? new ParsedBillEmail(outcome, ToParsedBill(payload.Bill), payload.Model)
            : ParsedBillEmail.Of(outcome, payload.Model);
    }

    public Task<FinancialRecommendations> RecommendAsync(
        RecommendationsInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return PostRecommendationsAsync(
            "/api/v1/financial-insights/recommendations",
            "ai-recommendations",
            ToRecommendRequest(input),
            cancellationToken);
    }

    public Task<FinancialRecommendations> RecommendBudgetAsync(
        BudgetRecommendationsInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return PostRecommendationsAsync(
            "/api/v1/budget-insights/recommendations",
            "ai-budget-recommendations",
            ToBudgetRecommendRequest(input),
            cancellationToken);
    }

    public Task<FinancialRecommendations> RecommendCompanyAsync(
        CompanyRecommendationsInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return PostRecommendationsAsync(
            "/api/v1/company-insights/recommendations",
            "ai-company-recommendations",
            ToCompanyRecommendRequest(input),
            cancellationToken);
    }

    private async Task<FinancialRecommendations> PostRecommendationsAsync<TRequest>(
        string path,
        string cachePrefix,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var cacheKey = RecommendationsCacheKey(cachePrefix, request);

        if (cacheKey is not null && _recommendationsCache!.TryGetValue(cacheKey, out FinancialRecommendations? cached) && cached is not null)
        {
            return cached;
        }

        using var response = await _httpClient.PostAsJsonAsync(path, request, cancellationToken);

        response.EnsureSuccessStatusCode();

        RecommendResponse? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<RecommendResponse>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("FinGrow-AI devolvio un JSON invalido al generar recomendaciones.", exception);
        }

        var recommendations = (payload?.Recommendations ?? new List<RecommendationPayload>())
            .Select(ToRecommendation)
            .OfType<FinancialRecommendation>()
            .ToList();

        if (recommendations.Count == 0)
        {
            throw new HttpRequestException("FinGrow-AI no devolvio ninguna recomendacion valida.");
        }

        var result = new FinancialRecommendations(recommendations, payload?.Model);

        if (cacheKey is not null)
        {
            _recommendationsCache!.Set(cacheKey, result, _recommendationsCacheDuration);
        }

        return result;
    }

    private string? RecommendationsCacheKey<TRequest>(string prefix, TRequest request)
    {
        if (_recommendationsCache is null || _recommendationsCacheDuration <= TimeSpan.Zero)
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));

        return $"{prefix}:{Convert.ToHexString(hash)}";
    }

    private static RecommendRequest ToRecommendRequest(RecommendationsInput input) => new(
        input.Language.ToString(),
        input.Currency.ToString(),
        input.PeriodStart,
        input.PeriodEnd,
        input.Months
            .Select(month => new RecommendMonthPayload(
                new DateOnly(month.Year, month.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                month.IsReference,
                month.Income,
                month.Expenses,
                month.Savings,
                month.ExpensesByCategory
                    .OrderBy(amount => amount.Key)
                    .Select(amount => new CategoryAmountPayload(amount.Key.ToWireValue(), amount.Value))
                    .ToList()))
            .ToList(),
        input.BudgetLimits
            .Select(limit => new BudgetLimitPayload(limit.Category.ToWireValue(), limit.Limit, limit.Spent))
            .ToList(),
        input.Goals
            .Select(goal => new GoalPayload(
                goal.Name,
                goal.IsAchieved ? "achieved" : "active",
                goal.TargetAmount,
                goal.CurrentAmount,
                goal.MonthlyCommitment,
                goal.Deadline))
            .ToList());

    private static BudgetRecommendRequest ToBudgetRecommendRequest(BudgetRecommendationsInput input) => new(
        input.Language.ToString(),
        input.Currency.ToString(),
        input.Months
            .Select(month => new BudgetMonthPayload(
                new DateOnly(month.Year, month.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                month.IsCurrent,
                month.Limits
                    .OrderBy(limit => limit.Category)
                    .Select(limit => new BudgetLimitPayload(limit.Category.ToWireValue(), limit.Limit, limit.Spent))
                    .ToList(),
                month.UnbudgetedExpenses
                    .Where(amount => amount.Value > 0m)
                    .OrderBy(amount => amount.Key)
                    .Select(amount => new CategoryAmountPayload(amount.Key.ToWireValue(), amount.Value))
                    .ToList()))
            .ToList());

    private static CompanyRecommendRequest ToCompanyRecommendRequest(CompanyRecommendationsInput input) => new(
        input.Language.ToString(),
        new DateOnly(input.Year, input.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
        input.MeasuredEmployees,
        input.AverageScore,
        input.ParticipationRate,
        input.Concerns
            .Select(concern => new CompanyConcernPayload(
                CompanyConcernWireValues[concern.Concern],
                concern.AffectedEmployees,
                concern.EvaluatedEmployees,
                concern.Percentage))
            .ToList());

    private static FinancialRecommendation? ToRecommendation(RecommendationPayload? recommendation)
    {
        if (recommendation is null
            || !RecommendationKinds.TryGetValue(recommendation.Kind ?? string.Empty, out var kind)
            || !RecommendationActions.TryGetValue(recommendation.Action ?? string.Empty, out var action)
            || string.IsNullOrWhiteSpace(recommendation.Title)
            || string.IsNullOrWhiteSpace(recommendation.Body))
        {
            return null;
        }

        return new FinancialRecommendation(kind, recommendation.Title.Trim(), recommendation.Body.Trim(), action);
    }

    private static ParsedBill ToParsedBill(ParsedBillPayload? bill)
    {
        if (bill is null
            || string.IsNullOrWhiteSpace(bill.Issuer)
            || bill.Amount <= 0
            || !Enum.TryParse<Currency>(bill.Currency, ignoreCase: false, out var currency)
            || !Enum.IsDefined(currency)
            || !ExpenseCategoryExtensions.TryFromWireValue(bill.Category, out var category)
            || !ConfidenceScale.IsValid(bill.Confidence))
        {
            throw new HttpRequestException("FinGrow-AI devolvio una factura incompleta.");
        }

        return new ParsedBill(bill.Issuer.Trim(), bill.Amount, currency, bill.DueDate, category, bill.Confidence);
    }

    public async Task<AudioTranscription> TranscribeAudioAsync(
        ReadOnlyMemory<byte> audio,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var file = new ReadOnlyMemoryContent(audio);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var mediaType)
            ? mediaType
            : new MediaTypeHeaderValue("application/octet-stream");

        using var form = new MultipartFormDataContent();
        form.Add(file, "file", "audio");

        using var response = await _httpClient.PostAsync("/api/v1/transcription/audio", form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var code = await ReadErrorCodeAsync(response, cancellationToken);

            if (code is not null && TranscriptionErrors.TryGetValue(code, out var failure))
            {
                return AudioTranscription.Of(failure);
            }

            response.EnsureSuccessStatusCode();
        }

        TranscriptionResponse? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<TranscriptionResponse>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("FinGrow-AI devolvio un JSON invalido al transcribir el audio.", exception);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Text))
        {
            throw new HttpRequestException("FinGrow-AI devolvio una transcripcion vacia.");
        }

        return new AudioTranscription(AudioTranscriptionOutcome.Transcribed, payload.Text.Trim(), payload.Model);
    }

    public async Task<ReceiptScan> ScanReceiptAsync(
        ReadOnlyMemory<byte> document,
        string contentType,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default)
    {
        var file = new ReadOnlyMemoryContent(document);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var mediaType)
            ? mediaType
            : new MediaTypeHeaderValue("application/octet-stream");

        using var form = new MultipartFormDataContent();
        form.Add(file, "file", "receipt");
        form.Add(new StringContent(defaultCurrency.ToString()), "default_currency");

        using var response = await _httpClient.PostAsync("/api/v1/receipt-scanning/receipts", form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var code = await ReadErrorCodeAsync(response, cancellationToken);

            if (code is not null && ReceiptScanErrors.TryGetValue(code, out var failure))
            {
                return ReceiptScan.Of(failure);
            }

            response.EnsureSuccessStatusCode();
        }

        ScanReceiptResponse? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<ScanReceiptResponse>(cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("FinGrow-AI devolvio un JSON invalido al escanear el comprobante.", exception);
        }

        if (payload is null || !ReceiptScanStatuses.TryGetValue(payload.Status ?? string.Empty, out var outcome))
        {
            throw new HttpRequestException("FinGrow-AI devolvio un status desconocido al escanear el comprobante.");
        }

        return outcome == ReceiptScanOutcome.Scanned
            ? new ReceiptScan(outcome, ToScannedReceipt(payload.Receipt), payload.Model)
            : ReceiptScan.Of(outcome, payload.Model);
    }

    private static ScannedReceipt ToScannedReceipt(ScannedReceiptPayload? receipt)
    {
        if (receipt is null
            || receipt.Amount <= 0
            || !Enum.TryParse<Currency>(receipt.Currency, ignoreCase: false, out var currency)
            || !Enum.IsDefined(currency)
            || !ExpenseCategoryExtensions.TryFromWireValue(receipt.Category, out var category)
            || !ConfidenceScale.IsValid(receipt.Confidence))
        {
            throw new HttpRequestException("FinGrow-AI devolvio un comprobante incompleto.");
        }

        PaymentMethod? paymentMethod = receipt.PaymentMethod is not null
            && PaymentMethods.TryGetValue(receipt.PaymentMethod, out var method)
                ? method
                : null;

        var merchant = string.IsNullOrWhiteSpace(receipt.Merchant) ? null : receipt.Merchant.Trim();

        return new ScannedReceipt(
            merchant,
            receipt.Amount,
            currency,
            ToDate(receipt.OccurredOn),
            category,
            paymentMethod,
            receipt.Confidence);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            return error?.Code;
        }
        catch (JsonException)
        {
            return null;
        }
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

        var occurredOn = ToDate(transaction.OccurredOn);

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
            transaction.Confidence,
            occurredOn,
            occurredOn is not null && transaction.OccurredOnIsEstimate == true);
    }

    private static DateOnly? ToDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private sealed record TranscriptionResponse(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record ScanReceiptResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("receipt")] ScannedReceiptPayload? Receipt,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record ScannedReceiptPayload(
        [property: JsonPropertyName("merchant")] string? Merchant,
        [property: JsonPropertyName("amount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("occurred_on")] string? OccurredOn,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("payment_method")] string? PaymentMethod,
        [property: JsonPropertyName("confidence")] double Confidence);

    private sealed record ErrorResponse([property: JsonPropertyName("code")] string? Code);

    private sealed record ParseMessageRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("default_currency")] string DefaultCurrency,
        [property: JsonPropertyName("today")] DateOnly Today);

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
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("occurred_on")] string? OccurredOn,
        [property: JsonPropertyName("occurred_on_is_estimate")] bool? OccurredOnIsEstimate);

    private sealed record ParseBillRequest(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("sender")] string Sender,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("default_currency")] string DefaultCurrency);

    private sealed record ParseBillResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("bill")] ParsedBillPayload? Bill,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record ParsedBillPayload(
        [property: JsonPropertyName("issuer")] string? Issuer,
        [property: JsonPropertyName("amount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("due_date")] DateOnly? DueDate,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("confidence")] double Confidence);

    private sealed record RecommendRequest(
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("period_start")] DateOnly PeriodStart,
        [property: JsonPropertyName("period_end")] DateOnly PeriodEnd,
        [property: JsonPropertyName("months")] List<RecommendMonthPayload> Months,
        [property: JsonPropertyName("budget_limits")] List<BudgetLimitPayload> BudgetLimits,
        [property: JsonPropertyName("goals")] List<GoalPayload> Goals);

    private sealed record RecommendMonthPayload(
        [property: JsonPropertyName("month")] string Month,
        [property: JsonPropertyName("is_reference")] bool IsReference,
        [property: JsonPropertyName("income")] decimal Income,
        [property: JsonPropertyName("expenses")] decimal Expenses,
        [property: JsonPropertyName("savings")] decimal Savings,
        [property: JsonPropertyName("expenses_by_category")] List<CategoryAmountPayload> ExpensesByCategory);

    private sealed record CategoryAmountPayload(
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("amount")] decimal Amount);

    private sealed record BudgetLimitPayload(
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("limit")] decimal Limit,
        [property: JsonPropertyName("spent")] decimal Spent);

    private sealed record GoalPayload(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("target_amount")] decimal TargetAmount,
        [property: JsonPropertyName("current_amount")] decimal CurrentAmount,
        [property: JsonPropertyName("monthly_commitment")] decimal MonthlyCommitment,
        [property: JsonPropertyName("deadline")] DateOnly Deadline);

    private sealed record BudgetRecommendRequest(
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("months")] List<BudgetMonthPayload> Months);

    private sealed record BudgetMonthPayload(
        [property: JsonPropertyName("month")] string Month,
        [property: JsonPropertyName("is_current")] bool IsCurrent,
        [property: JsonPropertyName("limits")] List<BudgetLimitPayload> Limits,
        [property: JsonPropertyName("unbudgeted_expenses")] List<CategoryAmountPayload> UnbudgetedExpenses);

    private sealed record CompanyRecommendRequest(
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("month")] string Month,
        [property: JsonPropertyName("measured_employees")] int MeasuredEmployees,
        [property: JsonPropertyName("average_score")] decimal? AverageScore,
        [property: JsonPropertyName("participation_rate")] decimal? ParticipationRate,
        [property: JsonPropertyName("concerns")] List<CompanyConcernPayload> Concerns);

    private sealed record CompanyConcernPayload(
        [property: JsonPropertyName("concern")] string Concern,
        [property: JsonPropertyName("affected_employees")] int AffectedEmployees,
        [property: JsonPropertyName("evaluated_employees")] int EvaluatedEmployees,
        [property: JsonPropertyName("percentage")] decimal Percentage);

    private sealed record RecommendResponse(
        [property: JsonPropertyName("recommendations")] List<RecommendationPayload>? Recommendations,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record RecommendationPayload(
        [property: JsonPropertyName("kind")] string? Kind,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("action")] string? Action);

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
