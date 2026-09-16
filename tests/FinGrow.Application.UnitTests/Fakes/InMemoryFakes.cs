namespace FinGrow.Application.UnitTests.Fakes;

using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;

public sealed class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Employees { get; } = new();

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Employees.FirstOrDefault(employee => employee.Id == id));

    public Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(Employees.FirstOrDefault(employee => employee.Email == email));
}

public sealed class FakeEmployeeIntegrationRepository : IEmployeeIntegrationRepository
{
    public List<EmployeeIntegration> Integrations { get; } = new();

    public Task<EmployeeIntegration?> FindByExternalAccountAsync(
        IntegrationProvider provider,
        string externalAccountId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Integrations.FirstOrDefault(integration =>
            integration.Provider == provider && integration.ExternalAccountId == externalAccountId));

    public Task<EmployeeIntegration?> FindByEmployeeAsync(
        Guid employeeId,
        IntegrationProvider provider,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Integrations.FirstOrDefault(integration =>
            integration.EmployeeId == employeeId && integration.Provider == provider));

    public void Add(EmployeeIntegration integration) => Integrations.Add(integration);

    public void Remove(EmployeeIntegration integration) => Integrations.Remove(integration);
}

public sealed class FakeIntegrationLinkCodeRepository : IIntegrationLinkCodeRepository
{
    public List<IntegrationLinkCode> LinkCodes { get; } = new();

    public Task<IntegrationLinkCode?> FindByHashAsync(
        IntegrationProvider provider,
        string codeHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(LinkCodes.FirstOrDefault(linkCode =>
            linkCode.Provider == provider && linkCode.CodeHash == codeHash));

    public void Add(IntegrationLinkCode linkCode) => LinkCodes.Add(linkCode);
}

public sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<Transaction> Transactions { get; } = new();

    public void Add(Transaction transaction) => Transactions.Add(transaction);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Transactions.FirstOrDefault(transaction => transaction.Id == id));

    public Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Transaction>>(Transactions
            .Where(transaction => transaction.EmployeeId == employeeId)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ToList());
}

public sealed class FakeTelegramBotClient : ITelegramBotClient
{
    public List<(long ChatId, string Text)> Sent { get; } = new();

    public bool Unreachable { get; set; }

    public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("api.telegram.org no responde");
        }

        Sent.Add((chatId, text));
        return Task.CompletedTask;
    }
}

public sealed class FakeTwilioMediaClient : ITwilioMediaClient
{
    public List<Uri> Downloaded { get; } = new();

    public Task<TwilioMedia> DownloadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        Downloaded.Add(url);
        return Task.FromResult(new TwilioMedia(new byte[] { 1, 2, 3 }, "audio/ogg"));
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public Guid? CompanyId { get; set; }

    public string? FullName { get; set; }

    public string? Role { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsAuthenticated => UserId is not null;
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => password;

    public bool Verify(string password, string hash) => password == hash;
}

public sealed class FakeTokenService : ITokenService
{
    public static readonly DateTimeOffset ExpiresAt = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    public AuthToken GenerateToken(Guid userId, Guid companyId, string role, string fullName) =>
        new($"token-for-{userId}", ExpiresAt);
}

public sealed class FakeMercadoPagoOAuthClient : IMercadoPagoOAuthClient
{
    public static readonly Uri AuthorizationBase = new("https://auth.mercadopago.test/authorization");

    public MercadoPagoTokens Tokens { get; set; } = new("access-token", "refresh-token", TimeSpan.FromDays(180), "228085066");

    public bool Unreachable { get; set; }

    public List<string> ExchangedCodes { get; } = new();

    public Uri BuildAuthorizationUrl(string state) => new(AuthorizationBase, $"?state={state}");

    public Task<MercadoPagoTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("Mercado Pago unreachable");
        }

        ExchangedCodes.Add(code);

        return Task.FromResult(Tokens);
    }

    public Task<MercadoPagoTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens);
}
