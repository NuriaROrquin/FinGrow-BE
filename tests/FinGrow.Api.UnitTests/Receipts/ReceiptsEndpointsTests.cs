namespace FinGrow.Api.UnitTests.Receipts;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class ReceiptsEndpointsTests
{
    private const string ReceiptsPath = "/api/receipts";
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };

    [Fact]
    public async Task Uploading_stores_the_file_and_persists_only_its_reference()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(new Uri(ReceiptsPath, UriKind.Relative), Upload(Jpeg, "image/jpeg"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var receipt = factory.Receipts.ShouldHaveSingleItem();
        receipt.EmployeeId.ShouldBe(factory.EmployeeId);
        receipt.TransactionId.ShouldBeNull();
        factory.Storage.Files[receipt.StorageKey].ShouldBe(Jpeg);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain($"\"id\":\"{receipt.Id}\"");
        body.ShouldContain("\"contentType\":\"image/jpeg\"");
        body.ShouldContain("\"fileName\":\"ticket.jpg\"");
        body.ShouldNotContain("storage");
    }

    [Fact]
    public async Task Uploading_a_file_whose_content_does_not_match_its_type_responds_400()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(
            new Uri(ReceiptsPath, UriKind.Relative),
            Upload("<html></html>"u8.ToArray(), "image/jpeg"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Receipts.ShouldBeEmpty();
        factory.Storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Uploading_an_unsupported_type_responds_400()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(
            new Uri(ReceiptsPath, UriKind.Relative),
            Upload("GIF89a"u8.ToArray(), "image/gif"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Uploading_responds_503_when_the_storage_does_not_respond()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        factory.Storage.Unreachable = true;
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(new Uri(ReceiptsPath, UriKind.Relative), Upload(Jpeg, "image/jpeg"));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        factory.Receipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Uploading_requires_a_session()
    {
        using var factory = new ReceiptsWebApplicationFactory();

        var response = await factory.CreateClient()
            .PostAsync(new Uri(ReceiptsPath, UriKind.Relative), Upload(Jpeg, "image/jpeg"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Downloading_returns_the_original_bytes_with_their_type()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var receipt = factory.AddStoredReceipt(factory.EmployeeId, Jpeg);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(FilePath(receipt.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(Jpeg);
    }

    [Fact]
    public async Task Downloading_a_receipt_of_another_employee_responds_403()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var receipt = factory.AddStoredReceipt(Guid.CreateVersion7(), Jpeg);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(FilePath(receipt.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Downloading_an_unknown_receipt_responds_404()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(FilePath(Guid.CreateVersion7()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Downloading_requires_a_session()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var receipt = factory.AddStoredReceipt(factory.EmployeeId, Jpeg);

        var response = await factory.CreateClient().GetAsync(FilePath(receipt.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Attaching_links_a_loose_receipt_to_a_transaction_loaded_by_hand()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var receipt = factory.AddStoredReceipt(factory.EmployeeId, Jpeg);
        var transaction = factory.AddExpense(factory.EmployeeId);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(AttachPath(receipt.Id), new { transactionId = transaction.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        receipt.TransactionId.ShouldBe(transaction.Id);
        (await response.Content.ReadAsStringAsync()).ShouldContain($"\"transactionId\":\"{transaction.Id}\"");
    }

    [Fact]
    public async Task Attaching_to_a_transaction_of_another_employee_responds_403()
    {
        using var factory = new ReceiptsWebApplicationFactory();
        var receipt = factory.AddStoredReceipt(factory.EmployeeId, Jpeg);
        var transaction = factory.AddExpense(Guid.CreateVersion7());
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(AttachPath(receipt.Id), new { transactionId = transaction.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        receipt.TransactionId.ShouldBeNull();
    }

    private static Uri FilePath(Guid id) => new($"{ReceiptsPath}/{id}/file", UriKind.Relative);

    private static Uri AttachPath(Guid id) => new($"{ReceiptsPath}/{id}/attach", UriKind.Relative);

    private static MultipartFormDataContent Upload(byte[] bytes, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent { { file, "file", "ticket.jpg" } };
    }

    private static HttpClient CreateAuthenticatedClient(ReceiptsWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(factory.EmployeeId, Guid.CreateVersion7(), Rol.Empleado, "Juan Perez");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class ReceiptsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Guid EmployeeId { get; } = Guid.CreateVersion7();

        public List<TransactionReceipt> Receipts { get; } = new();

        public List<Transaction> Transactions { get; } = new();

        public InMemoryFileStorage Storage { get; } = new();

        public TransactionReceipt AddStoredReceipt(Guid employeeId, byte[] content)
        {
            var receipt = TransactionReceipt.Upload(employeeId, "image/jpeg", content.Length, "ticket.jpg", Now);
            Receipts.Add(receipt);
            Storage.Files[receipt.StorageKey] = content;

            return receipt;
        }

        public Transaction AddExpense(Guid employeeId)
        {
            var transaction = Transaction.RegisterExpense(
                employeeId,
                Money.From(4500m, Currency.ARS),
                ExpenseCategory.Alimentos,
                "Supermercado",
                new DateOnly(2026, 10, 7),
                PaymentMethod.DebitCard,
                TransactionSource.Manual,
                TransactionStatus.Confirmed,
                Now);
            Transactions.Add(transaction);

            return transaction;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = "unit-test-twilio-auth-token",
                    ["Telegram:BotToken"] = "unit-test-telegram-bot-token",
                    ["Telegram:WebhookSecret"] = "unit-test-telegram-secret",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<ITransactionReceiptRepository>(_ => new InMemoryReceiptRepository(Receipts));
                services.AddScoped<ITransactionRepository>(_ => new InMemoryTransactionRepository(Transactions));
                services.AddSingleton<IFileStorage>(Storage);
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class InMemoryFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

        public bool Unreachable { get; set; }

        public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            EnsureReachable();

            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            Files[key] = copy.ToArray();
        }

        public Task<StoredFile?> OpenAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureReachable();

            return Task.FromResult(Files.TryGetValue(key, out var content)
                ? new StoredFile(new MemoryStream(content), "application/octet-stream", content.Length)
                : null);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Files.Remove(key);
            return Task.CompletedTask;
        }

        private void EnsureReachable()
        {
            if (Unreachable)
            {
                throw new FileStorageUnavailableException("el almacenamiento no responde");
            }
        }
    }

    private sealed class InMemoryReceiptRepository(List<TransactionReceipt> receipts) : ITransactionReceiptRepository
    {
        public void Add(TransactionReceipt receipt) => receipts.Add(receipt);

        public Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(receipts.FirstOrDefault(receipt => receipt.Id == id));
    }

    private sealed class InMemoryTransactionRepository(List<Transaction> transactions) : ITransactionRepository
    {
        public void Add(Transaction transaction) => transactions.Add(transaction);

        public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(transactions.FirstOrDefault(transaction => transaction.Id == id));

        public Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(transactions.Where(t => t.EmployeeId == employeeId).ToList());

        public Task<IReadOnlyList<Transaction>> ListPendingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(transactions.Where(t => t.EmployeeId == employeeId && t.IsPending).ToList());

        public Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(
            Guid employeeId,
            TransactionSource source,
            IReadOnlyCollection<string> externalReferences,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
