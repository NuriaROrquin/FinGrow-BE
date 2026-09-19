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

public sealed class FakeCompanyRepository : ICompanyRepository
{
    public List<Company> Companies { get; } = new();

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Companies.FirstOrDefault(company => company.Id == id));

    public Task<Company?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(Companies.FirstOrDefault(company => company.Email == email));
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

    public Task<IReadOnlyList<EmployeeIntegration>> ListAuthorizedAsync(IntegrationProvider provider, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EmployeeIntegration>>(
            Integrations.Where(integration => integration.Provider == provider && integration.Grant is not null).ToList());

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

    public Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(Guid employeeId, TransactionSource source, IReadOnlyCollection<string> externalReferences, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(Transactions
            .Where(transaction => transaction.EmployeeId == employeeId && transaction.Source == source && transaction.ExternalReference is not null && externalReferences.Contains(transaction.ExternalReference))
            .Select(transaction => transaction.ExternalReference!)
            .ToHashSet(StringComparer.Ordinal));

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

public sealed class FakeMercadoPagoPaymentsClient : IMercadoPagoPaymentsClient
{
    public List<MercadoPagoPayment> Payments { get; } = new();

    public List<(DateTimeOffset From, DateTimeOffset To, int Offset)> Searches { get; } = new();

    public int PageSize { get; set; } = 50;

    public Task<MercadoPagoPaymentsPage> SearchUpdatedBetweenAsync(string accessToken, DateTimeOffset from, DateTimeOffset to, int offset, CancellationToken cancellationToken = default)
    {
        Searches.Add((from, to, offset));

        return Task.FromResult(new MercadoPagoPaymentsPage(Payments.Skip(offset).Take(PageSize).ToList(), Payments.Count));
    }
}

public sealed class FakeAiService : IAiService
{
    public Dictionary<string, ExpenseCategory> CategoriesByDescription { get; } = new(StringComparer.Ordinal);

    public bool Unreachable { get; set; }

    public List<ExpenseToCategorize> Received { get; } = new();

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(!Unreachable);

    public Task<IReadOnlyList<CategorizedExpense>> CategorizeExpensesAsync(IReadOnlyList<ExpenseToCategorize> expenses, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        Received.AddRange(expenses);

        return Task.FromResult<IReadOnlyList<CategorizedExpense>>(expenses
            .Where(expense => CategoriesByDescription.ContainsKey(expense.Description))
            .Select(expense => new CategorizedExpense(expense.Id, CategoriesByDescription[expense.Description], 0.9))
            .ToList());
    }
}

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = new();

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public void Add(RefreshToken refreshToken) => Tokens.Add(refreshToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(t => t.UserId == userId && t.RevokedAt is null))
        {
            token.Revoke(revokedAt);
        }

        return Task.CompletedTask;
    }
}
