namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Ai;

public class AiServiceBillParsingTests
{
    private static readonly BillEmail Email = new("Tu factura", "edenor.com.ar", "TOTAL A PAGAR $73.456,92");

    [Fact]
    public async Task The_email_and_the_default_currency_are_sent_to_FinGrow_AI()
    {
        using var handler = new RecordingHandler("""{"status": "not_a_bill", "bill": null, "model": "m"}""");

        var parsed = await Parse(handler, Currency.USD);

        parsed.Outcome.ShouldBe(BillParsingOutcome.NotABill);
        handler.Path.ShouldBe("/api/v1/email-parsing/bills");
        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("subject").GetString().ShouldBe("Tu factura");
        body.RootElement.GetProperty("sender").GetString().ShouldBe("edenor.com.ar");
        body.RootElement.GetProperty("body").GetString().ShouldBe("TOTAL A PAGAR $73.456,92");
        body.RootElement.GetProperty("default_currency").GetString().ShouldBe("USD");
    }

    [Fact]
    public async Task A_bill_is_mapped_to_the_domain_types()
    {
        using var handler = new RecordingHandler("""
            {
              "status": "bill",
              "bill": {
                "issuer": " Edenor ", "amount": "73456.92", "currency": "ARS",
                "due_date": "2026-10-01", "category": "servicios", "confidence": 0.95
              },
              "model": "claude-sonnet-5"
            }
            """);

        var parsed = await Parse(handler);

        parsed.Outcome.ShouldBe(BillParsingOutcome.Bill);
        parsed.Model.ShouldBe("claude-sonnet-5");
        parsed.Bill.ShouldBe(new ParsedBill("Edenor", 73456.92m, Currency.ARS, new DateOnly(2026, 10, 1), ExpenseCategory.Servicios, 0.95));
    }

    [Fact]
    public async Task A_bill_without_due_date_keeps_it_null()
    {
        using var handler = new RecordingHandler("""
            {"status": "bill", "bill": {"issuer": "Personal", "amount": 9999, "currency": "ARS",
             "due_date": null, "category": "servicios", "confidence": 0.8}, "model": "m"}
            """);

        var parsed = await Parse(handler);

        parsed.Bill!.DueDate.ShouldBeNull();
    }

    [Theory]
    [InlineData("""{"status": "bill", "bill": null, "model": "m"}""")]
    [InlineData("""{"status": "bill", "bill": {"issuer": "Edenor", "amount": 10, "currency": "ARS", "category": "inventada", "confidence": 0.9}, "model": "m"}""")]
    [InlineData("""{"status": "quien sabe", "bill": null, "model": "m"}""")]
    [InlineData("no es json")]
    public async Task An_incoherent_answer_is_an_http_failure(string response)
    {
        using var handler = new RecordingHandler(response);

        await Should.ThrowAsync<HttpRequestException>(() => Parse(handler));
    }

    private static async Task<ParsedBillEmail> Parse(RecordingHandler handler, Currency currency = Currency.ARS)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fingrow-ai.test") };

        return await new AiService(httpClient).ParseBillEmailAsync(Email, currency);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;

        public RecordingHandler(string response) => _response = response;

        public string? Path { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
