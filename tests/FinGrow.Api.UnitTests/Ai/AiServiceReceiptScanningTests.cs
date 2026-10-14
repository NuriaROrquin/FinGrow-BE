namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Ai;

public class AiServiceReceiptScanningTests
{
    private const string Parsed = """
        {
          "status": "parsed",
          "receipt": {
            "merchant": "  Supermercado Día  ",
            "amount": "1250.50",
            "currency": "ARS",
            "occurred_on": "2026-10-03",
            "category": "alimentos",
            "payment_method": "debit_card",
            "confidence": 0.93
          },
          "model": "textract-analyze-expense+claude-sonnet-5"
        }
        """;

    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

    [Fact]
    public async Task The_document_and_the_default_currency_are_sent_as_a_multipart_request()
    {
        using var handler = new RecordingHandler(Parsed);

        await Scan(handler, "image/jpeg", Currency.USD);

        handler.Path.ShouldBe("/api/v1/receipt-scanning/receipts");
        handler.Parts["file"].ContentType.ShouldBe("image/jpeg");
        handler.Parts["file"].Bytes.ShouldBe(Jpeg);
        Encoding.UTF8.GetString(handler.Parts["default_currency"].Bytes).ShouldBe("USD");
    }

    [Fact]
    public async Task A_parsed_receipt_returns_its_fields_and_the_model()
    {
        using var handler = new RecordingHandler(Parsed);

        var scan = await Scan(handler);

        scan.Outcome.ShouldBe(ReceiptScanOutcome.Scanned);
        scan.Model.ShouldBe("textract-analyze-expense+claude-sonnet-5");
        scan.Receipt.ShouldBe(new ScannedReceipt(
            "Supermercado Día",
            1250.50m,
            Currency.ARS,
            new DateOnly(2026, 10, 3),
            ExpenseCategory.Alimentos,
            PaymentMethod.DebitCard,
            0.93));
    }

    [Fact]
    public async Task Missing_merchant_date_and_payment_method_come_back_as_null()
    {
        using var handler = new RecordingHandler("""
            {
              "status": "parsed",
              "receipt": {"merchant": null, "amount": 890, "currency": "ARS", "occurred_on": null,
                          "category": "salud", "payment_method": null, "confidence": 0.5},
              "model": "m"
            }
            """);

        var scan = await Scan(handler);

        var receipt = scan.Receipt.ShouldNotBeNull();
        receipt.Merchant.ShouldBeNull();
        receipt.OccurredOn.ShouldBeNull();
        receipt.PaymentMethod.ShouldBeNull();
    }

    [Theory]
    [InlineData("missing_amount", ReceiptScanOutcome.MissingAmount)]
    [InlineData("unreadable", ReceiptScanOutcome.Unreadable)]
    public async Task A_receipt_without_a_total_or_without_text_is_an_expected_outcome(
        string status,
        ReceiptScanOutcome expected)
    {
        using var handler = new RecordingHandler($$"""{"status": "{{status}}", "receipt": null, "model": "m"}""");

        var scan = await Scan(handler);

        scan.ShouldBe(ReceiptScan.Of(expected, "m"));
    }

    [Theory]
    [InlineData("document_not_readable", HttpStatusCode.UnprocessableEntity, ReceiptScanOutcome.Unreadable)]
    [InlineData("unsupported_document", HttpStatusCode.UnsupportedMediaType, ReceiptScanOutcome.UnsupportedDocument)]
    [InlineData("document_too_large", HttpStatusCode.RequestEntityTooLarge, ReceiptScanOutcome.TooLarge)]
    public async Task A_document_the_ocr_rejects_is_an_expected_outcome(
        string code,
        HttpStatusCode status,
        ReceiptScanOutcome expected)
    {
        using var handler = new RecordingHandler($$"""{"code": "{{code}}", "message": "x"}""", status);

        var scan = await Scan(handler);

        scan.ShouldBe(ReceiptScan.Of(expected));
    }

    [Theory]
    [InlineData("""{"code": "model_unavailable", "message": "caido"}""", HttpStatusCode.ServiceUnavailable)]
    [InlineData("""{"code": "inference_failed", "message": "x"}""", HttpStatusCode.BadGateway)]
    [InlineData("<html>Bad Gateway</html>", HttpStatusCode.BadGateway)]
    public async Task Any_other_error_from_FinGrow_AI_is_an_http_failure(string body, HttpStatusCode status)
    {
        using var handler = new RecordingHandler(body, status);

        await Should.ThrowAsync<HttpRequestException>(() => Scan(handler));
    }

    [Theory]
    [InlineData("""{"status": "parsed", "receipt": null, "model": "m"}""")]
    [InlineData("""{"status": "parsed", "receipt": {"amount": 0, "currency": "ARS", "category": "alimentos", "confidence": 0.9}, "model": "m"}""")]
    [InlineData("""{"status": "parsed", "receipt": {"amount": 10, "currency": "ARS", "category": "golosinas", "confidence": 0.9}, "model": "m"}""")]
    [InlineData("""{"status": "parsed", "receipt": {"amount": 10, "currency": "XYZ", "category": "alimentos", "confidence": 0.9}, "model": "m"}""")]
    [InlineData("""{"status": "otro", "model": "m"}""")]
    [InlineData("esto no es json")]
    public async Task An_incomplete_or_unknown_answer_is_an_http_failure(string body)
    {
        using var handler = new RecordingHandler(body);

        await Should.ThrowAsync<HttpRequestException>(() => Scan(handler));
    }

    private static async Task<ReceiptScan> Scan(
        RecordingHandler handler,
        string contentType = "image/jpeg",
        Currency defaultCurrency = Currency.ARS)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fingrow-ai.test") };

        return await new AiService(httpClient).ScanReceiptAsync(Jpeg, contentType, defaultCurrency);
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

        public Dictionary<string, (string? ContentType, byte[] Bytes)> Parts { get; } = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;

            if (request.Content is MultipartFormDataContent form)
            {
                foreach (var part in form)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"') ?? string.Empty;
                    Parts[name] = (part.Headers.ContentType?.ToString(), await part.ReadAsByteArrayAsync(cancellationToken));
                }
            }

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
