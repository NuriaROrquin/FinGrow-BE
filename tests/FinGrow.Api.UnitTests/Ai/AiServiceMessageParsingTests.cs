namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Ai;

public class AiServiceMessageParsingTests
{
    [Fact]
    public async Task The_text_and_the_default_currency_are_sent_to_FinGrow_AI()
    {
        using var handler = new RecordingHandler("""{"status": "not_a_transaction", "transaction": null, "model": "m"}""");

        await Parse(handler, "Gasté $500 en el super", Currency.USD);

        handler.Path.ShouldBe("/api/v1/message-parsing/transactions");
        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("text").GetString().ShouldBe("Gasté $500 en el super");
        body.RootElement.GetProperty("default_currency").GetString().ShouldBe("USD");
    }

    [Fact]
    public async Task A_parsed_expense_is_mapped_to_the_domain_enums()
    {
        using var handler = new RecordingHandler("""
            {
              "status": "parsed",
              "transaction": {
                "type": "expense", "amount": "1500.50", "currency": "ARS",
                "expense_category": "ahorro_inversion", "income_category": null,
                "description": "Plazo fijo", "payment_method": "bank_transfer", "confidence": 0.7
              },
              "model": "claude-sonnet-5"
            }
            """);

        var parsed = await Parse(handler);

        parsed.Outcome.ShouldBe(MessageParsingOutcome.Parsed);
        parsed.Model.ShouldBe("claude-sonnet-5");
        parsed.Transaction.ShouldBe(new ParsedTransaction(
            TransactionType.Expense, 1500.50m, Currency.ARS, ExpenseCategory.AhorroInversion, null, "Plazo fijo", PaymentMethod.BankTransfer, 0.7));
    }

    [Fact]
    public async Task A_parsed_income_without_payment_method_keeps_it_null()
    {
        using var handler = new RecordingHandler("""
            {
              "status": "parsed",
              "transaction": {
                "type": "income", "amount": 10000, "currency": "ARS",
                "expense_category": null, "income_category": "freelance",
                "description": "Freelance", "payment_method": null, "confidence": 0.9
              },
              "model": "m"
            }
            """);

        var parsed = await Parse(handler);

        parsed.Transaction!.IncomeCategory.ShouldBe(IncomeCategory.Freelance);
        parsed.Transaction.ExpenseCategory.ShouldBeNull();
        parsed.Transaction.PaymentMethod.ShouldBeNull();
    }

    [Theory]
    [InlineData("missing_amount", MessageParsingOutcome.MissingAmount)]
    [InlineData("not_a_transaction", MessageParsingOutcome.NotATransaction)]
    [InlineData("multiple_transactions", MessageParsingOutcome.MultipleTransactions)]
    public async Task Statuses_without_a_transaction_are_mapped(string status, MessageParsingOutcome expected)
    {
        using var handler = new RecordingHandler($$"""{"status": "{{status}}", "transaction": null, "model": "m"}""");

        var parsed = await Parse(handler);

        parsed.Outcome.ShouldBe(expected);
        parsed.Transaction.ShouldBeNull();
    }

    [Theory]
    [InlineData("""{"status": "otra_cosa", "model": "m"}""")]
    [InlineData("""{"status": "parsed", "transaction": null, "model": "m"}""")]
    [InlineData("""{"status": "parsed", "transaction": {"type": "expense", "amount": 5, "currency": "ARS", "expense_category": null, "income_category": "salario", "description": "x", "confidence": 0.5}, "model": "m"}""")]
    [InlineData("""{"status": "parsed", "transaction": {"type": "expense", "amount": 5, "currency": "JPY", "expense_category": "otros", "description": "x", "confidence": 0.5}, "model": "m"}""")]
    [InlineData("esto no es json")]
    public async Task An_answer_that_cannot_be_saved_is_reported_as_an_http_failure(string json)
    {
        using var handler = new RecordingHandler(json);

        await Should.ThrowAsync<HttpRequestException>(() => Parse(handler));
    }

    [Fact]
    public async Task An_error_status_from_FinGrow_AI_is_an_http_failure()
    {
        using var handler = new RecordingHandler("""{"code": "model_unavailable", "message": "caido"}""", HttpStatusCode.ServiceUnavailable);

        await Should.ThrowAsync<HttpRequestException>(() => Parse(handler));
    }

    private static async Task<ParsedMessage> Parse(RecordingHandler handler, string text = "texto", Currency currency = Currency.ARS)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fingrow-ai.test") };

        return await new AiService(httpClient).ParseTransactionMessageAsync(text, currency);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _status;

        public RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK)
        {
            _response = response;
            _status = status;
        }

        public string? Path { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
