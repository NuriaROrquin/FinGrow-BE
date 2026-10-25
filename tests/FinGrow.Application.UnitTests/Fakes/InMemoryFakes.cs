namespace FinGrow.Application.UnitTests.Fakes;

using System.Globalization;
using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Investments.ListInvestments;
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

    public Task<IReadOnlyDictionary<Guid, int>> CountByDepartmentAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(Employees
            .Where(employee => employee.CompanyId == companyId && employee.DepartmentId is not null)
            .GroupBy(employee => employee.DepartmentId!.Value)
            .ToDictionary(group => group.Key, group => group.Count()));

    public Task<IReadOnlyList<Employee>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Employee>>(Employees
            .Where(employee => employee.CompanyId == companyId)
            .OrderBy(employee => employee.FullName, StringComparer.Ordinal)
            .ToList());

    public Task<IReadOnlyList<Employee>> ListByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Employee>>(Employees.Where(employee => employee.DepartmentId == departmentId).ToList());

    public Task<IReadOnlyList<Employee>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Employee>>(Employees.Where(employee => ids.Contains(employee.Id)).ToList());

    public void Add(Employee employee) => Employees.Add(employee);

    public void Remove(Employee employee) => Employees.Remove(employee);
}

public sealed class FakeCompanyRepository : ICompanyRepository
{
    public List<Company> Companies { get; } = new();

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Companies.FirstOrDefault(company => company.Id == id));

    public Task<Company?> GetWithDepartmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<Company?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(Companies.FirstOrDefault(company => company.Email == email));

    public Task<IReadOnlyList<Company>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Company>>(Companies.Where(company => company.IsActive).ToList());
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
            Integrations.Where(integration =>
                integration.Provider == provider
                && integration.Grant is not null
                && integration.Status == IntegrationStatus.Active).ToList());

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
        Task.FromResult(Transactions.FirstOrDefault(
            transaction => transaction.Id == id
                && transaction.Status != TransactionStatus.Eliminated
                && transaction.Status != TransactionStatus.Discarded));

    public Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(Guid employeeId, TransactionSource source, IReadOnlyCollection<string> externalReferences, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(Transactions
            .Where(transaction => transaction.EmployeeId == employeeId && transaction.Source == source && transaction.ExternalReference is not null && externalReferences.Contains(transaction.ExternalReference))
            .Select(transaction => transaction.ExternalReference!)
            .ToHashSet(StringComparer.Ordinal));

    public Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Transaction>>(Transactions
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status != TransactionStatus.Eliminated
                && transaction.Status != TransactionStatus.Discarded)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ToList());

    public Task<IReadOnlyList<Transaction>> ListPendingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Transaction>>(Transactions
            .Where(transaction => transaction.EmployeeId == employeeId && transaction.Status == TransactionStatus.Pending)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .ToList());
}

public sealed class FakeTelegramBotClient : ITelegramBotClient
{
    public List<(long ChatId, string Text)> Sent { get; } = new();

    public List<(long ChatId, string Text, IReadOnlyList<IReadOnlyList<TelegramButton>> ButtonRows)> SentWithButtons { get; } = new();

    public List<(long ChatId, long MessageId, string Text)> Edited { get; } = new();

    public List<(string CallbackQueryId, string? Text)> AnsweredCallbacks { get; } = new();

    public bool Unreachable { get; set; }

    public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();

        Sent.Add((chatId, text));
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(
        long chatId,
        string text,
        IReadOnlyList<IReadOnlyList<TelegramButton>> buttonRows,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();

        Sent.Add((chatId, text));
        SentWithButtons.Add((chatId, text, buttonRows));
        return Task.CompletedTask;
    }

    public Task EditMessageAsync(long chatId, long messageId, string text, CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();

        Edited.Add((chatId, messageId, text));
        return Task.CompletedTask;
    }

    public Task AnswerCallbackAsync(string callbackQueryId, string? text = null, CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();

        AnsweredCallbacks.Add((callbackQueryId, text));
        return Task.CompletedTask;
    }

    public List<string> DownloadedFiles { get; } = new();

    public bool FilesUnavailable { get; set; }

    public Task<ReadOnlyMemory<byte>> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();

        if (FilesUnavailable)
        {
            throw new HttpRequestException("api.telegram.org no devuelve el archivo");
        }

        DownloadedFiles.Add(fileId);
        return Task.FromResult<ReadOnlyMemory<byte>>(FileContent);
    }

    public byte[] FileContent { get; set; } = new byte[] { 1, 2, 3 };

    private void ThrowIfUnreachable()
    {
        if (Unreachable)
        {
            throw new HttpRequestException("api.telegram.org no responde");
        }
    }
}

public sealed class FakeTwilioMediaClient : ITwilioMediaClient
{
    public List<Uri> Downloaded { get; } = new();

    public bool Unreachable { get; set; }

    public Task<TwilioMedia> DownloadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("Twilio unreachable");
        }

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

    public static readonly DateTimeOffset ChallengeExpiresAt = new(2026, 9, 15, 10, 5, 0, TimeSpan.Zero);

    private const string ChallengePrefix = "challenge-for-";

    public AuthToken GenerateToken(Guid userId, Guid companyId, string role, string fullName) =>
        new($"token-for-{userId}", ExpiresAt);

    public AuthToken GenerateTwoFactorChallenge(Guid employeeId) =>
        new($"{ChallengePrefix}{employeeId}", ChallengeExpiresAt);

    public Guid? ReadTwoFactorChallenge(string challengeToken) =>
        challengeToken.StartsWith(ChallengePrefix, StringComparison.Ordinal)
        && Guid.TryParse(challengeToken[ChallengePrefix.Length..], out var employeeId)
            ? employeeId
            : null;
}

public sealed class FakeTotpService : ITotpService
{
    public const string ValidCode = "123456";

    public string NextSecret { get; set; } = "SECRETBASE32";

    public string GenerateSecret() => NextSecret;

    public bool VerifyCode(string secret, string code, DateTimeOffset now) => code == ValidCode;

    public string BuildProvisioningUri(string secret, string accountName) => $"otpauth://totp/FinGrow:{accountName}?secret={secret}";
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

public sealed class FakeGmailMailboxClient : IGmailMailboxClient
{
    public List<GmailMessage> Messages { get; } = new();

    public HashSet<string> RejectedTokens { get; } = new(StringComparer.Ordinal);

    public int PageSize { get; set; } = 100;

    public List<(string AccessToken, string Query)> Searches { get; } = new();

    public List<string> ReadMessages { get; } = new();

    public Task<GmailMessagePage> SearchAsync(string accessToken, string query, string? pageToken, CancellationToken cancellationToken = default)
    {
        ThrowIfRejected(accessToken);
        Searches.Add((accessToken, query));

        var skip = pageToken is null ? 0 : int.Parse(pageToken, CultureInfo.InvariantCulture);
        var newestFirst = Messages.OrderByDescending(message => message.ReceivedAt).Select(message => message.Id).ToList();
        var page = newestFirst.Skip(skip).Take(PageSize).ToList();
        var next = skip + PageSize < newestFirst.Count ? (skip + PageSize).ToString(CultureInfo.InvariantCulture) : null;

        return Task.FromResult(new GmailMessagePage(page, next));
    }

    public Task<GmailMessage> GetMessageAsync(string accessToken, string messageId, CancellationToken cancellationToken = default)
    {
        ThrowIfRejected(accessToken);
        ReadMessages.Add(messageId);

        return Task.FromResult(Messages.Single(message => message.Id == messageId));
    }

    private void ThrowIfRejected(string accessToken)
    {
        if (RejectedTokens.Contains(accessToken))
        {
            throw new HttpRequestException("Gmail respondio 401", inner: null, HttpStatusCode.Unauthorized);
        }
    }
}

public sealed class FakeGmailOAuthClient : IGmailOAuthClient
{
    public static readonly Uri AuthorizationBase = new("https://accounts.google.test/o/oauth2/v2/auth");

    public GmailTokens Tokens { get; set; } = new("access-token", "refresh-token", TimeSpan.FromHours(1), CanReadMail: true);

    public string EmailAddress { get; set; } = "juan.perez@gmail.com";

    public bool Unreachable { get; set; }

    public List<string> ExchangedCodes { get; } = new();

    public List<string> RevokedTokens { get; } = new();

    public Uri BuildAuthorizationUrl(string state) => new(AuthorizationBase, $"?state={state}");

    public Task<GmailTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();
        ExchangedCodes.Add(code);

        return Task.FromResult(Tokens);
    }

    public Exception? RefreshFailure { get; set; }

    public List<string> RefreshedTokens { get; } = new();

    public Task<GmailTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (RefreshFailure is not null)
        {
            throw RefreshFailure;
        }

        RefreshedTokens.Add(refreshToken);

        return Task.FromResult(Tokens);
    }

    public Task<string> GetEmailAddressAsync(string accessToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(EmailAddress);

    public Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        ThrowIfUnreachable();
        RevokedTokens.Add(token);

        return Task.CompletedTask;
    }

    private void ThrowIfUnreachable()
    {
        if (Unreachable)
        {
            throw new HttpRequestException("Google unreachable");
        }
    }
}

public sealed class FakeIolAuthClient : IIolAuthClient
{
    public IolTokens Tokens { get; set; } = new("iol-access", "iol-refresh", TimeSpan.FromMinutes(15));

    public Exception? Failure { get; set; }

    public List<(string Username, string Password)> Logins { get; } = new();

    public List<string> Refreshes { get; } = new();

    public Task<IolTokens> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        Logins.Add((username, password));

        return Failure is null ? Task.FromResult(Tokens) : Task.FromException<IolTokens>(Failure);
    }

    public Task<IolTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        Refreshes.Add(refreshToken);

        return Failure is null ? Task.FromResult(Tokens) : Task.FromException<IolTokens>(Failure);
    }
}

public sealed class FakeMercadoPagoPaymentsClient : IMercadoPagoPaymentsClient
{
    public List<MercadoPagoPayment> Payments { get; } = new();

    public List<(DateTimeOffset From, DateTimeOffset To, int Offset)> Searches { get; } = new();

    public int PageSize { get; set; } = 50;

    public string? BrokenAccessToken { get; set; }

    public string? UnauthorizedAccessToken { get; set; }

    public Task<MercadoPagoPaymentsPage> SearchUpdatedBetweenAsync(string accessToken, DateTimeOffset from, DateTimeOffset to, int offset, CancellationToken cancellationToken = default)
    {
        if (accessToken == BrokenAccessToken)
        {
            throw new InvalidOperationException("Mercado Pago devolvio una respuesta que no se pudo leer");
        }

        if (accessToken == UnauthorizedAccessToken)
        {
            throw new HttpRequestException("Response status code does not indicate success: 401 (Unauthorized).", null, System.Net.HttpStatusCode.Unauthorized);
        }

        Searches.Add((from, to, offset));

        return Task.FromResult(new MercadoPagoPaymentsPage(Payments.Skip(offset).Take(PageSize).ToList(), Payments.Count));
    }
}

public sealed class FakeIolPortfolioClient : IIolPortfolioClient
{
    public Dictionary<IolCountry, List<IolPosition>> Portfolios { get; } = new()
    {
        [IolCountry.Argentina] = new(),
        [IolCountry.UnitedStates] = new(),
    };

    public List<IolOperation> Operations { get; } = new();

    public Exception? PortfolioFailure { get; set; }

    public IolCountry? FailingCountry { get; set; }

    public string? UnavailableAccessToken { get; set; }

    public Exception? OperationsFailure { get; set; }

    public List<(string AccessToken, IolCountry Country)> PortfolioRequests { get; } = new();

    public List<DateOnly> OperationRequests { get; } = new();

    public Task<IReadOnlyList<IolPosition>> GetPortfolioAsync(string accessToken, IolCountry country, CancellationToken cancellationToken = default)
    {
        PortfolioRequests.Add((accessToken, country));

        if (accessToken == UnavailableAccessToken)
        {
            return Task.FromException<IReadOnlyList<IolPosition>>(
                new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).", null, System.Net.HttpStatusCode.BadGateway));
        }

        if (PortfolioFailure is not null && (FailingCountry is null || FailingCountry == country))
        {
            return Task.FromException<IReadOnlyList<IolPosition>>(PortfolioFailure);
        }

        return Task.FromResult<IReadOnlyList<IolPosition>>(Portfolios[country].ToList());
    }

    public Task<IReadOnlyList<IolOperation>> ListCompletedOperationsAsync(string accessToken, DateOnly from, CancellationToken cancellationToken = default)
    {
        OperationRequests.Add(from);

        return OperationsFailure is null
            ? Task.FromResult<IReadOnlyList<IolOperation>>(Operations.ToList())
            : Task.FromException<IReadOnlyList<IolOperation>>(OperationsFailure);
    }
}

public sealed class FakeAiService : IAiService
{
    public Dictionary<string, ExpenseCategory> CategoriesByDescription { get; } = new(StringComparer.Ordinal);

    public bool Unreachable { get; set; }

    public double Confidence { get; set; } = 0.9;

    public string? Model { get; set; } = "fake-model";

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
            .Select(expense => new CategorizedExpense(expense.Id, CategoriesByDescription[expense.Description], Confidence, Model))
            .ToList());
    }

    public ParsedMessage ParseResult { get; set; } = ParsedMessage.Of(MessageParsingOutcome.NotATransaction, "fake-model");

    public List<(string Text, Currency DefaultCurrency, DateOnly Today)> ParsedMessages { get; } = new();

    public Task<ParsedMessage> ParseTransactionMessageAsync(string text, Currency defaultCurrency, DateOnly today, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        ParsedMessages.Add((text, defaultCurrency, today));

        return Task.FromResult(ParseResult);
    }

    public Func<BillEmail, ParsedBillEmail> ParseBill { get; set; } = _ => ParsedBillEmail.Of(BillParsingOutcome.NotABill, "fake-model");

    public List<(BillEmail Email, Currency DefaultCurrency)> ParsedEmails { get; } = new();

    public Task<ParsedBillEmail> ParseBillEmailAsync(BillEmail email, Currency defaultCurrency, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        ParsedEmails.Add((email, defaultCurrency));

        return Task.FromResult(ParseBill(email));
    }

    public AudioTranscription TranscriptionResult { get; set; } =
        new(AudioTranscriptionOutcome.Transcribed, "Gasté 500 en el super", "fake-transcribe");

    public List<(byte[] Audio, string ContentType)> TranscribedAudios { get; } = new();

    public bool TranscriptionHangs { get; set; }

    public async Task<AudioTranscription> TranscribeAudioAsync(ReadOnlyMemory<byte> audio, string contentType, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        if (TranscriptionHangs)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        TranscribedAudios.Add((audio.ToArray(), contentType));

        return TranscriptionResult;
    }

    public ReceiptScan ScanResult { get; set; } = new(
        ReceiptScanOutcome.Scanned,
        new ScannedReceipt(
            "Supermercado Día",
            1250.50m,
            Currency.ARS,
            new DateOnly(2026, 10, 3),
            ExpenseCategory.Alimentos,
            PaymentMethod.DebitCard,
            0.93),
        "fake-ocr+fake-model");

    public List<(byte[] Document, string ContentType, Currency DefaultCurrency)> ScannedReceipts { get; } = new();

    public Task<ReceiptScan> ScanReceiptAsync(
        ReadOnlyMemory<byte> document,
        string contentType,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        ScannedReceipts.Add((document.ToArray(), contentType, defaultCurrency));

        return Task.FromResult(ScanResult);
    }

    public FinancialRecommendations RecommendationsResult { get; set; } = new(
        new[]
        {
            new FinancialRecommendation(RecommendationKind.Positive, "Vas bien", "Ahorraste el 20% de tus ingresos.", RecommendationAction.Goals),
            new FinancialRecommendation(RecommendationKind.Warning, "Cuidado con alimentos", "Superaste el tope del mes.", RecommendationAction.Budgets),
            new FinancialRecommendation(RecommendationKind.Tip, "Registra todo", "Cargá tus gastos chicos.", RecommendationAction.Transactions),
        },
        "fake-model");

    public List<RecommendationsInput> RecommendationRequests { get; } = new();

    public Task<FinancialRecommendations> RecommendAsync(RecommendationsInput input, CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        RecommendationRequests.Add(input);

        return Task.FromResult(RecommendationsResult);
    }

    public List<BudgetRecommendationsInput> BudgetRecommendationRequests { get; } = new();

    public Task<FinancialRecommendations> RecommendBudgetAsync(
        BudgetRecommendationsInput input,
        CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        BudgetRecommendationRequests.Add(input);

        return Task.FromResult(RecommendationsResult);
    }

    public List<CompanyRecommendationsInput> CompanyRecommendationRequests { get; } = new();

    public Task<FinancialRecommendations> RecommendCompanyAsync(
        CompanyRecommendationsInput input,
        CancellationToken cancellationToken = default)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("FinGrow-AI unreachable");
        }

        CompanyRecommendationRequests.Add(input);

        return Task.FromResult(RecommendationsResult);
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

public sealed class FakeGoalRepository : IGoalRepository
{
    public List<Goal> Goals { get; } = new();

    public void Add(Goal goal) => Goals.Add(goal);

    public Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Goals.FirstOrDefault(goal => goal.Id == id));

    public Task<IReadOnlyList<Goal>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Goal>>(Goals
            .Where(goal => goal.EmployeeId == employeeId)
            .OrderByDescending(goal => goal.CreatedAt)
            .ToList());

    public Task<IReadOnlyList<Goal>> ListByEmployeesAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Goal>>(Goals
            .Where(goal => employeeIds.Contains(goal.EmployeeId))
            .ToList());

    public Task<IReadOnlyList<Guid>> ListEmployeesWithActiveGoalsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Goals
            .Where(goal => goal.Status == GoalStatus.Active)
            .Select(goal => goal.EmployeeId)
            .Distinct()
            .ToList());
}

public sealed class FakeJobRunRepository : IJobRunRepository
{
    public List<JobRun> Runs { get; } = new();

    public void Add(JobRun run) => Runs.Add(run);

    public Task<JobRun?> FindLastAsync(string jobName, CancellationToken cancellationToken = default) =>
        Task.FromResult(MostRecentFirst(jobName).FirstOrDefault());

    public Task<bool> HasSucceededAsync(string jobName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Runs.Any(run => run.JobName == jobName && run.Status == JobRunStatus.Succeeded));

    public Task<IReadOnlyList<JobRun>> ListRecentAsync(string jobName, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JobRun>>(MostRecentFirst(jobName).Take(take).ToList());

    private IEnumerable<JobRun> MostRecentFirst(string jobName) =>
        Runs.Where(run => run.JobName == jobName)
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id);
}

public sealed class FakeMetricsSnapshotRepository : IMetricsSnapshotRepository
{
    public List<CompanyMetricsSnapshot> CompanySnapshots { get; } = new();

    public List<DepartmentMetricsSnapshot> DepartmentSnapshots { get; } = new();

    public Task<IReadOnlySet<DateOnly>> ListCompanyPeriodStartsAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<DateOnly>>(CompanySnapshots
            .Where(snapshot => snapshot.CompanyId == companyId)
            .Select(snapshot => snapshot.PeriodStart)
            .ToHashSet());

    public Task<CompanyMetricsSnapshot?> FindCompanyAsync(Guid companyId, DateOnly periodStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(CompanySnapshots.SingleOrDefault(snapshot => snapshot.CompanyId == companyId && snapshot.PeriodStart == periodStart));

    public Task<DepartmentMetricsSnapshot?> FindDepartmentAsync(Guid departmentId, DateOnly periodStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(DepartmentSnapshots.SingleOrDefault(snapshot => snapshot.DepartmentId == departmentId && snapshot.PeriodStart == periodStart));

    public Task<IReadOnlyList<CompanyMetricsSnapshot>> ListCompanyAsync(
        Guid companyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyMetricsSnapshot>>(CompanySnapshots
            .Where(snapshot => snapshot.CompanyId == companyId
                && (from == null || snapshot.PeriodStart >= from)
                && (to == null || snapshot.PeriodStart <= to))
            .OrderBy(snapshot => snapshot.PeriodStart)
            .ToList());

    public Task<IReadOnlyList<DepartmentMetricsSnapshot>> ListDepartmentAsync(
        Guid companyId,
        Guid departmentId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DepartmentMetricsSnapshot>>(DepartmentSnapshots
            .Where(snapshot => snapshot.CompanyId == companyId
                && snapshot.DepartmentId == departmentId
                && (from == null || snapshot.PeriodStart >= from)
                && (to == null || snapshot.PeriodStart <= to))
            .OrderBy(snapshot => snapshot.PeriodStart)
            .ToList());

    public Task<IReadOnlyList<DepartmentMetricsSnapshot>> ListDepartmentsAsync(
        Guid companyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DepartmentMetricsSnapshot>>(DepartmentSnapshots
            .Where(snapshot => snapshot.CompanyId == companyId
                && (from == null || snapshot.PeriodStart >= from)
                && (to == null || snapshot.PeriodStart <= to))
            .OrderBy(snapshot => snapshot.PeriodStart)
            .ThenBy(snapshot => snapshot.DepartmentId)
            .ToList());

    public void Add(CompanyMetricsSnapshot snapshot) => CompanySnapshots.Add(snapshot);

    public void Add(DepartmentMetricsSnapshot snapshot) => DepartmentSnapshots.Add(snapshot);
}

public sealed class FakePeriodActivityReadRepository : IPeriodActivityReadRepository
{
    public Dictionary<(Guid CompanyId, DateOnly PeriodStart), List<EmployeePeriodActivity>> Activity { get; } = new();

    public List<(Guid CompanyId, MetricsPeriod Period)> Requests { get; } = new();

    public Task<IReadOnlyList<EmployeePeriodActivity>> ListByCompanyAsync(Guid companyId, MetricsPeriod period, CancellationToken cancellationToken = default)
    {
        Requests.Add((companyId, period));

        return Task.FromResult<IReadOnlyList<EmployeePeriodActivity>>(
            Activity.TryGetValue((companyId, period.Start), out var employees) ? employees : new List<EmployeePeriodActivity>());
    }
}

public sealed class FakeFinancialHealthReadRepository : IFinancialHealthReadRepository
{
    public List<EmployeeCurrencyTotals> Totals { get; } = new();

    public List<EmployeeBudgetLimit> Limits { get; } = new();

    public Task<IReadOnlyList<EmployeeCurrencyTotals>> ListMovementTotalsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        MetricsPeriod period,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EmployeeCurrencyTotals>>(Totals
            .Where(total => employeeIds.Contains(total.EmployeeId))
            .ToList());

    public Task<IReadOnlyList<EmployeeBudgetLimit>> ListBudgetLimitsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        MetricsPeriod period,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EmployeeBudgetLimit>>(Limits
            .Where(limit => employeeIds.Contains(limit.EmployeeId))
            .ToList());
}

public sealed class FakeFinancialHealthScoreRepository : IFinancialHealthScoreRepository
{
    public List<FinancialHealthScore> Scores { get; } = new();

    /// <summary>Empresa de cada empleado con puntaje; uno sin entrada cuenta para cualquier empresa.</summary>
    public Dictionary<Guid, Guid> CompanyByEmployee { get; } = new();

    public Task<IReadOnlyList<FinancialHealthScore>> ListByCompanyAsync(
        Guid companyId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FinancialHealthScore>>(Scores
            .Where(score => score.PeriodStart == periodStart
                && (!CompanyByEmployee.TryGetValue(score.EmployeeId, out var owner) || owner == companyId))
            .ToList());

    public void Add(FinancialHealthScore score) => Scores.Add(score);

    public void Remove(FinancialHealthScore score) => Scores.Remove(score);
}

public sealed class FakeCompanyEducationMetricsReadRepository : ICompanyEducationMetricsReadRepository
{
    public CompanyEducationMetrics Metrics { get; set; } = new(0, 0, 0, 0);

    public Dictionary<(Guid CompanyId, DateOnly PeriodStart), int> CompletedCourses { get; } = new();

    public Task<CompanyEducationMetrics> GetAsync(
        Guid companyId,
        DateOnly last30DaysStart,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Metrics);

    public Task<int> CountCompletedCoursesAsync(
        Guid companyId,
        MetricsPeriod period,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CompletedCourses.TryGetValue((companyId, period.Start), out var count) ? count : 0);
}

public sealed class FakeInvestmentRepository : IInvestmentRepository
{
    public List<Investment> Investments { get; } = new();

    public void Add(Investment investment) => Investments.Add(investment);

    public void Remove(Investment investment) => Investments.Remove(investment);

    public Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Investments.FirstOrDefault(investment => investment.Id == id));

    public Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Investment>>(Investments
            .Where(investment => investment.EmployeeId == employeeId)
            .OrderByDescending(investment => investment.PurchasedOn)
            .ThenByDescending(investment => investment.CreatedAt)
            .ToList());

    public Task<IReadOnlyList<Guid>> ListEmployeesWithTrackedInvestmentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Investments
            .Where(investment => investment.Source == InvestmentSource.Manual && investment.Symbol is not null)
            .Select(investment => investment.EmployeeId)
            .Distinct()
            .ToList());

    public Task<IReadOnlyList<Investment>> ListTrackedByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Investment>>(Investments
            .Where(investment => investment.EmployeeId == employeeId
                && investment.Source == InvestmentSource.Manual
                && investment.Symbol is not null)
            .ToList());

    public Task<IReadOnlyList<Guid>> ListEmployeesWithAccruingInvestmentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Investments
            .Where(investment => investment.AnnualRate is not null)
            .Select(investment => investment.EmployeeId)
            .Distinct()
            .ToList());

    public Task<IReadOnlyList<Investment>> ListAccruingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Investment>>(Investments
            .Where(investment => investment.EmployeeId == employeeId && investment.AnnualRate is not null)
            .ToList());

    public Task<IReadOnlyList<Investment>> ListImportedAsync(Guid employeeId, InvestmentSource source, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Investment>>(Investments
            .Where(investment => investment.EmployeeId == employeeId && investment.Source == source)
            .ToList());
}

public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    public MepQuote Quote { get; set; } = new(1544.30m, 1557.30m, new DateTimeOffset(2026, 9, 27, 14, 57, 0, TimeSpan.Zero), "DolarApi");

    public Exception? Failure { get; set; }

    public int Calls { get; private set; }

    public Task<MepQuote> GetMepQuoteAsync(CancellationToken cancellationToken = default)
    {
        Calls++;

        return Failure is null ? Task.FromResult(Quote) : Task.FromException<MepQuote>(Failure);
    }
}

public sealed class FakeInvestmentReadRepository : IInvestmentReadRepository
{
    public List<Investment> Investments { get; } = new();

    public Guid? RequestedEmployeeId { get; private set; }

    public InvestmentFilters? RequestedFilters { get; private set; }

    public Task<PagedResult<Investment>> GetPageAsync(Guid employeeId, InvestmentFilters filters, CancellationToken cancellationToken = default)
    {
        RequestedEmployeeId = employeeId;
        RequestedFilters = filters;

        var owned = Investments
            .Where(investment => investment.EmployeeId == employeeId)
            .OrderByDescending(investment => investment.PurchasedOn)
            .ToList();

        return Task.FromResult(new PagedResult<Investment>(
            owned.Skip((filters.PageNumber - 1) * filters.PageSize).Take(filters.PageSize).ToList(),
            filters.PageNumber,
            filters.PageSize,
            owned.Count));
    }
}

public sealed class FakeMarketPriceProvider : IMarketPriceProvider
{
    public List<MarketPrice> Prices { get; } = new();

    public Exception? Failure { get; set; }

    public int Calls { get; private set; }

    public PriceMarket Market { get; set; } = PriceMarket.Exchange;

    public string Source { get; set; } = "BYMA";

    public Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default)
    {
        Calls++;

        return Failure is null
            ? Task.FromResult<IReadOnlyList<MarketPrice>>(Prices.ToList())
            : Task.FromException<IReadOnlyList<MarketPrice>>(Failure);
    }
}

public sealed class FakeSecurityPriceRepository : ISecurityPriceRepository
{
    public List<SecurityPrice> Prices { get; } = new();

    public Task<SecurityPrice?> FindAsync(
        PriceMarket market,
        string symbol,
        Currency currency,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Prices.FirstOrDefault(price =>
            price.Market == market
            && price.Symbol == symbol
            && (market.IgnoresCurrency() || price.Currency == currency)));

    public Task<IReadOnlyList<SecurityPrice>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SecurityPrice>>(Prices.ToList());

    public void AddRange(IEnumerable<SecurityPrice> prices) => Prices.AddRange(prices);
}

public sealed class FakeBudgetRepository : IBudgetRepository
{
    public List<Budget> Budgets { get; } = new();

    public void Add(Budget budget) => Budgets.Add(budget);

    public void Remove(Budget budget) => Budgets.Remove(budget);

    public Task<bool> ExistsForPeriodAsync(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Budgets.Exists(budget =>
            budget.EmployeeId == employeeId && budget.Period == period && budget.PeriodStart == periodStart));

    public Task<Budget?> FindForPeriodAsync(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Budgets.FirstOrDefault(budget =>
            budget.EmployeeId == employeeId && budget.Period == period && budget.PeriodStart == periodStart));

    public Task<IReadOnlyList<Budget>> ListMonthlyAsync(
        Guid employeeId,
        DateOnly fromPeriodStart,
        DateOnly toPeriodStart,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Budget>>(Budgets
            .Where(budget => budget.EmployeeId == employeeId
                && budget.Period == BudgetPeriod.Monthly
                && budget.PeriodStart >= fromPeriodStart
                && budget.PeriodStart <= toPeriodStart)
            .OrderBy(budget => budget.PeriodStart)
            .ToList());
}

public sealed class FakeBudgetSpendingReadRepository : IBudgetSpendingReadRepository
{
    public Dictionary<ExpenseCategory, decimal> Spent { get; } = new();

    public Task<IReadOnlyDictionary<ExpenseCategory, decimal>> GetSpentByCategoryAsync(
        Budget budget,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<ExpenseCategory, decimal>>(new Dictionary<ExpenseCategory, decimal>(Spent));
}

public sealed class FakeCourseRepository : ICourseRepository
{
    public List<Course> Courses { get; } = new();

    public List<LessonCompletion> Completions { get; } = new();

    public void Add(Course course) => Courses.Add(course);

    public void Complete(Guid employeeId, Lesson lesson, DateTimeOffset? completedAt = null) =>
        Completions.Add(LessonCompletion.Create(employeeId, lesson.Id, completedAt ?? DateTimeOffset.UnixEpoch));

    public Task<IReadOnlyList<Course>> ListPublishedAsync(
        CourseLevel? level,
        int? maxDurationMinutes,
        InvestmentType? relatedInvestmentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Course>>(Courses
            .Where(course => course.IsPublished)
            .Where(course => level is null || course.Level == level)
            .Where(course => maxDurationMinutes is null || course.DurationMinutes <= maxDurationMinutes)
            .Where(course => relatedInvestmentType is null || course.RelatedInvestmentType == relatedInvestmentType)
            .OrderBy(course => course.Level)
            .ThenBy(course => course.Title)
            .ToList());

    public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult(Courses.FirstOrDefault(course => course.Slug == slug && course.IsPublished));

    public Task<IReadOnlySet<Guid>> ListCompletedLessonIdsAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<Guid>>(Completions
            .Where(completion => completion.EmployeeId == employeeId)
            .Select(completion => completion.LessonId)
            .ToHashSet());

    public Task<IReadOnlyList<DateTimeOffset>> ListCompletionDatesAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DateTimeOffset>>(Completions
            .Where(completion => completion.EmployeeId == employeeId)
            .Select(completion => completion.CompletedAt)
            .ToList());

    public void AddCompletion(LessonCompletion completion) => Completions.Add(completion);
}

public sealed class FakeArticleRepository : IArticleRepository
{
    public List<Article> Articles { get; } = new();

    public void Add(Article article) => Articles.Add(article);

    public Task<IReadOnlyList<Article>> ListPublishedAsync(
        EducationCategory? category,
        int? maxReadingTimeMinutes,
        InvestmentType? relatedInvestmentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Article>>(Articles
            .Where(article => article.IsPublished)
            .Where(article => category is null || article.Category == category)
            .Where(article => maxReadingTimeMinutes is null || article.ReadingTimeMinutes <= maxReadingTimeMinutes)
            .Where(article => relatedInvestmentType is null || article.RelatedInvestmentType == relatedInvestmentType)
            .OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Title)
            .ToList());

    public Task<Article?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult(Articles.FirstOrDefault(article => article.Slug == slug && article.IsPublished));
}

public sealed class FakeCourseRatingRepository : ICourseRatingRepository, ICourseRatingReadRepository
{
    public List<CourseRating> Ratings { get; } = new();

    public Task<CourseRating?> FindAsync(Guid employeeId, Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Ratings.FirstOrDefault(rating => rating.EmployeeId == employeeId && rating.CourseId == courseId));

    public void Add(CourseRating rating) => Ratings.Add(rating);

    public Task<IReadOnlyDictionary<Guid, CourseRatingSummary>> GetSummariesAsync(
        Guid employeeId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, CourseRatingSummary>>(Ratings
            .Where(rating => courseIds.Contains(rating.CourseId))
            .GroupBy(rating => rating.CourseId)
            .ToDictionary(
                group => group.Key,
                group => new CourseRatingSummary(
                    group.Count(),
                    group.Average(rating => (decimal)rating.Score),
                    group.FirstOrDefault(rating => rating.EmployeeId == employeeId)?.Score)));
}

public sealed class FakeNotificationRepository : INotificationRepository
{
    public List<Notification> Notifications { get; } = new();

    public void Add(Notification notification) => Notifications.Add(notification);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Notifications.FirstOrDefault(notification => notification.Id == id));

    public Task<bool> ExistsSinceAsync(
        NotificationRecipient recipient,
        NotificationType type,
        string deduplicationKey,
        DateTimeOffset since,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Notifications.Any(notification =>
            notification.IsAddressedTo(recipient)
            && notification.Type == type
            && notification.DeduplicationKey == deduplicationKey
            && notification.CreatedAt >= since));

    public Task<IReadOnlyList<Notification>> ListAsync(
        NotificationRecipient recipient,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Notification>>(Filtered(recipient, unreadOnly)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip(skip)
            .Take(take)
            .ToList());

    public Task<int> CountAsync(NotificationRecipient recipient, bool unreadOnly, CancellationToken cancellationToken = default) =>
        Task.FromResult(Filtered(recipient, unreadOnly).Count());

    public Task<IReadOnlyList<Notification>> ListUnreadAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Notification>>(Filtered(recipient, unreadOnly: true).ToList());

    public Task RemoveAllForAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        Notifications.RemoveAll(notification => notification.IsAddressedTo(recipient));
        return Task.CompletedTask;
    }

    private IEnumerable<Notification> Filtered(NotificationRecipient recipient, bool unreadOnly) =>
        Notifications.Where(notification => notification.IsAddressedTo(recipient) && (!unreadOnly || !notification.IsRead));
}

public sealed class FakeNotificationChannelSettingRepository : INotificationChannelSettingRepository
{
    public List<NotificationChannelSetting> Settings { get; } = new();

    public void Add(NotificationChannelSetting setting) => Settings.Add(setting);

    public Task<IReadOnlyList<NotificationChannelSetting>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationChannelSetting>>(Settings.Where(setting => setting.EmployeeId == employeeId).ToList());
}

public sealed class FakeNotificationTypeSettingRepository : INotificationTypeSettingRepository
{
    public List<NotificationTypeSetting> Settings { get; } = new();

    public void Add(NotificationTypeSetting setting) => Settings.Add(setting);

    public Task<IReadOnlyList<NotificationTypeSetting>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationTypeSetting>>(Settings.Where(setting => setting.EmployeeId == employeeId).ToList());
}

public sealed class FakeCompanyNotificationSettingRepository : ICompanyNotificationSettingRepository
{
    public List<CompanyNotificationSetting> Settings { get; } = new();

    public void Add(CompanyNotificationSetting setting) => Settings.Add(setting);

    public Task<IReadOnlyList<CompanyNotificationSetting>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyNotificationSetting>>(Settings.Where(setting => setting.CompanyId == companyId).ToList());
}

public sealed class FakeTransactionReceiptRepository : ITransactionReceiptRepository
{
    public List<TransactionReceipt> Receipts { get; } = new();

    public void Add(TransactionReceipt receipt) => Receipts.Add(receipt);

    public void Remove(TransactionReceipt receipt) => Receipts.Remove(receipt);

    public Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Receipts.FirstOrDefault(receipt => receipt.Id == id));

    public Task<TransactionReceipt?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Receipts.FirstOrDefault(receipt => receipt.TransactionId == transactionId));

    public Task<IReadOnlyDictionary<Guid, Guid>> GetIdsByTransactionAsync(
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Guid>>(Receipts
            .Where(receipt => receipt.TransactionId is { } id && transactionIds.Contains(id))
            .ToDictionary(receipt => receipt.TransactionId!.Value, receipt => receipt.Id));

    public Task<IReadOnlyList<string>> ListStorageKeysByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Receipts
            .Where(receipt => receipt.EmployeeId == employeeId)
            .Select(receipt => receipt.StorageKey)
            .ToList());

    public Task<IReadOnlyList<TransactionReceipt>> ListDetachedUploadedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TransactionReceipt>>(Receipts
            .Where(receipt => receipt.TransactionId is null && receipt.UploadedAt < cutoff)
            .OrderBy(receipt => receipt.UploadedAt)
            .ToList());
}

public sealed class FakeFileStorage : IFileStorage
{
    public Dictionary<string, (byte[] Content, string ContentType)> Files { get; } = new(StringComparer.Ordinal);

    public bool Unreachable { get; set; }

    public HashSet<string> UndeletableKeys { get; } = new(StringComparer.Ordinal);

    public Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureReachable();

        using var copy = new MemoryStream();
        content.CopyTo(copy);
        Files[key] = (copy.ToArray(), contentType);

        return Task.CompletedTask;
    }

    public Task<StoredFile?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureReachable();

        return Task.FromResult(Files.TryGetValue(key, out var file)
            ? new StoredFile(new MemoryStream(file.Content), file.ContentType, file.Content.Length)
            : null);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureReachable();

        if (UndeletableKeys.Contains(key))
        {
            throw new FileStorageUnavailableException($"el almacenamiento no pudo borrar {key}");
        }

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

public sealed class FakeOutboundEmailRepository : IOutboundEmailRepository
{
    public List<OutboundEmail> Emails { get; } = new();

    public void Add(OutboundEmail email) => Emails.Add(email);

    public Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Emails.FirstOrDefault(email => email.Id == id));

    public Task<IReadOnlyList<Guid>> ListDueIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Emails
            .Where(email => email.IsAwaitingDelivery && email.NextAttemptAt <= now)
            .OrderBy(email => email.NextAttemptAt)
            .Select(email => email.Id)
            .Take(take)
            .ToList());
}

public sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = new();

    public EmailDeliveryException? Failure { get; set; }

    public Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Sent.Add(message);
        return Task.FromResult($"re-{Sent.Count}");
    }
}

public sealed class FakeMonthlySummaryReadRepository : IMonthlySummaryReadRepository
{
    public List<(Guid EmployeeId, DateOnly OccurredOn, TransactionType Type, ExpenseCategory? Category, Money Amount)> Movements { get; } = new();

    public void Income(Guid employeeId, DateOnly occurredOn, decimal amount, Currency currency = Currency.ARS) =>
        Movements.Add((employeeId, occurredOn, TransactionType.Income, null, Money.From(amount, currency)));

    public void Expense(Guid employeeId, DateOnly occurredOn, decimal amount, ExpenseCategory category, Currency currency = Currency.ARS) =>
        Movements.Add((employeeId, occurredOn, TransactionType.Expense, category, Money.From(amount, currency)));

    public Task<IReadOnlyList<MonthlyMovementTotal>> ListMonthlyTotalsAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MonthlyMovementTotal>>(Movements
            .Where(movement => movement.EmployeeId == employeeId
                && movement.Amount.Currency == currency
                && movement.OccurredOn >= fromDate
                && movement.OccurredOn <= toDate)
            .GroupBy(movement => (movement.OccurredOn.Year, movement.OccurredOn.Month, movement.Type, movement.Category))
            .Select(group => new MonthlyMovementTotal(
                group.Key.Year,
                group.Key.Month,
                group.Key.Type,
                group.Key.Category,
                group.Sum(movement => movement.Amount.Amount)))
            .ToList());

    public Task<DateOnly?> FindFirstMovementDateAsync(Guid employeeId, Currency currency, CancellationToken cancellationToken = default) =>
        Task.FromResult(Movements
            .Where(movement => movement.EmployeeId == employeeId && movement.Amount.Currency == currency)
            .Select(movement => (DateOnly?)movement.OccurredOn)
            .Min());

    public Task<IReadOnlyList<MonthlyActivity>> ListMonthlyActivityAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MonthlyActivity>>(Movements
            .Where(movement => movement.EmployeeId == employeeId)
            .GroupBy(movement => (movement.OccurredOn.Year, movement.OccurredOn.Month))
            .Select(group => new MonthlyActivity(
                group.Key.Year,
                group.Key.Month,
                group.Count(),
                group.Any(movement => movement.Type == TransactionType.Income)))
            .ToList());
}

public sealed class FakeAchievementRepository : IAchievementRepository
{
    public List<EmployeeAchievement> Achievements { get; } = new();

    public void Add(EmployeeAchievement achievement) => Achievements.Add(achievement);

    public Task<IReadOnlyList<EmployeeAchievement>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EmployeeAchievement>>(Achievements
            .Where(achievement => achievement.EmployeeId == employeeId)
            .OrderBy(achievement => achievement.AchievedAt)
            .ToList());

    public Task<IReadOnlyList<EmployeeAchievement>> ListUncelebratedAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EmployeeAchievement>>(Achievements
            .Where(achievement => achievement.EmployeeId == employeeId && !achievement.IsCelebrated)
            .OrderBy(achievement => achievement.AchievedAt)
            .ToList());
}

public sealed class FakeAchievementProgressReadRepository : IAchievementProgressReadRepository
{
    public Dictionary<(Guid EmployeeId, AchievementCode Code), DateTimeOffset> Reached { get; } = new();

    public List<(Guid EmployeeId, IReadOnlyCollection<AchievementCode> Codes)> Requests { get; } = new();

    public Exception? Failure { get; set; }

    public void Reach(Guid employeeId, AchievementCode code, DateTimeOffset reachedAt) => Reached[(employeeId, code)] = reachedAt;

    public Task<IReadOnlyDictionary<AchievementCode, DateTimeOffset>> ListReachedAsync(
        Guid employeeId,
        IReadOnlyCollection<AchievementCode> codes,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            return Task.FromException<IReadOnlyDictionary<AchievementCode, DateTimeOffset>>(Failure);
        }

        Requests.Add((employeeId, codes.ToList()));

        return Task.FromResult<IReadOnlyDictionary<AchievementCode, DateTimeOffset>>(codes
            .Where(code => Reached.ContainsKey((employeeId, code)))
            .ToDictionary(code => code, code => Reached[(employeeId, code)]));
    }
}
