namespace FinGrow.Api.UnitTests.Transactions;

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

public class PendingTransactionsEndpointsTests
{
    private const string PendingPath = "/api/transactions/pending";
    private static readonly DateTimeOffset ProposedAt = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task The_inbox_shows_the_proposal_with_its_confidence_level()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        proposal.SuggestExpenseCategory(ExpenseCategory.Alimentos, 0.95, "claude-sonnet-5", ProposedAt);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri(PendingPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"totalCount\":1");
        body.ShouldContain("\"type\":\"Expense\"");
        body.ShouldContain("\"amount\":15400.50");
        body.ShouldContain("\"occurredOn\":\"2026-10-04\"");
        body.ShouldContain("\"description\":\"Supermercado Coto\"");
        body.ShouldContain("\"category\":\"Alimentos\"");
        body.ShouldContain("\"aiConfidence\":0.95");
        body.ShouldContain("\"aiConfidenceLevel\":\"High\"");
        body.ShouldContain("\"source\":\"MercadoPago\"");
    }

    [Fact]
    public async Task The_inbox_does_not_show_proposals_of_another_employee()
    {
        using var factory = new TransactionsWebApplicationFactory();
        factory.AddPendingExpense(Guid.CreateVersion7());
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri(PendingPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"totalCount\":0");
    }

    [Fact]
    public async Task The_inbox_requires_a_session()
    {
        using var factory = new TransactionsWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync(new Uri(PendingPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Confirming_with_corrections_stores_the_corrected_values()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(
            ConfirmPath(proposal.Id),
            Correction(amount: 18250m, category: "Alimentos", description: "Compra mensual en Coto"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        proposal.Status.ShouldBe(TransactionStatus.Confirmed);
        proposal.Amount.ShouldBe(Money.From(18250m, Currency.ARS));
        proposal.ExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        proposal.Description.ShouldBe("Compra mensual en Coto");
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"status\":\"Confirmed\"");
    }

    [Fact]
    public async Task Confirming_with_an_invalid_amount_responds_400_and_keeps_it_pending()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(ConfirmPath(proposal.Id), Correction(amount: 0m));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Confirming_a_transaction_already_confirmed_responds_409()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        proposal.Confirm(ProposedAt.AddHours(1));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(ConfirmPath(proposal.Id), Correction());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Confirming_a_proposal_of_another_employee_responds_403()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(Guid.CreateVersion7());
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(ConfirmPath(proposal.Id), Correction());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Confirming_requires_a_session()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);

        var response = await factory.CreateClient().PostAsJsonAsync(ConfirmPath(proposal.Id), Correction());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Discarding_a_proposal_takes_it_out_of_the_inbox_without_confirming_it()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(DiscardPath(proposal.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        proposal.Status.ShouldBe(TransactionStatus.Discarded);
        var inbox = await client.GetAsync(new Uri(PendingPath, UriKind.Relative));
        (await inbox.Content.ReadAsStringAsync()).ShouldContain("\"totalCount\":0");
    }

    [Fact]
    public async Task Discarding_twice_responds_404_the_second_time()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        var client = CreateAuthenticatedClient(factory);
        await client.PostAsync(DiscardPath(proposal.Id), content: null);

        var response = await client.PostAsync(DiscardPath(proposal.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Discarding_a_confirmed_transaction_responds_409()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(factory.EmployeeId);
        proposal.Confirm(ProposedAt.AddHours(1));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(DiscardPath(proposal.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        proposal.Status.ShouldBe(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task Discarding_a_proposal_of_another_employee_responds_403()
    {
        using var factory = new TransactionsWebApplicationFactory();
        var proposal = factory.AddPendingExpense(Guid.CreateVersion7());
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsync(DiscardPath(proposal.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
    }

    private static Uri ConfirmPath(Guid id) => new($"/api/transactions/{id}/confirm", UriKind.Relative);

    private static Uri DiscardPath(Guid id) => new($"/api/transactions/{id}/discard", UriKind.Relative);

    private static object Correction(
        decimal amount = 15400.50m,
        string category = "Otros",
        string description = "Supermercado Coto") => new
    {
        type = "Expense",
        amount,
        currency = "ARS",
        expenseCategory = category,
        incomeCategory = (string?)null,
        description,
        occurredOn = "2026-10-04",
        paymentMethod = "DebitCard",
    };

    private static HttpClient CreateAuthenticatedClient(TransactionsWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(factory.EmployeeId, Guid.CreateVersion7(), Rol.Empleado, "Juan Perez");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class TransactionsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Guid EmployeeId { get; } = Guid.CreateVersion7();

        public List<Transaction> Transactions { get; } = new();

        public Transaction AddPendingExpense(Guid employeeId)
        {
            var transaction = Transaction.RegisterExpense(
                employeeId,
                Money.From(15400.50m, Currency.ARS),
                ExpenseCategory.Otros,
                "Supermercado Coto",
                new DateOnly(2026, 10, 4),
                PaymentMethod.DebitCard,
                TransactionSource.MercadoPago,
                TransactionStatus.Pending,
                ProposedAt,
                externalReference: "mp-1");
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
                services.AddScoped<ITransactionRepository>(_ => new InMemoryTransactionRepository(Transactions));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class InMemoryTransactionRepository(List<Transaction> transactions) : ITransactionRepository
    {
        public void Add(Transaction transaction) => transactions.Add(transaction);

        public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(transactions.FirstOrDefault(transaction =>
                transaction.Id == id
                && transaction.Status != TransactionStatus.Eliminated
                && transaction.Status != TransactionStatus.Discarded));

        public Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(transactions
                .Where(transaction =>
                    transaction.EmployeeId == employeeId
                    && transaction.Status != TransactionStatus.Eliminated
                    && transaction.Status != TransactionStatus.Discarded)
                .ToList());

        public Task<IReadOnlyList<Transaction>> ListPendingByEmployeeAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(transactions
                .Where(transaction => transaction.EmployeeId == employeeId && transaction.IsPending)
                .ToList());

        public Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(
            Guid employeeId,
            TransactionSource source,
            IReadOnlyCollection<string> externalReferences,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(transactions
                .Where(transaction =>
                    transaction.EmployeeId == employeeId
                    && transaction.Source == source
                    && transaction.ExternalReference is not null
                    && externalReferences.Contains(transaction.ExternalReference))
                .Select(transaction => transaction.ExternalReference!)
                .ToHashSet(StringComparer.Ordinal));
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
