namespace FinGrow.Application.UnitTests.Fakes;

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
}

public sealed class FakeCompanyRepository : ICompanyRepository
{
    public List<Company> Companies { get; } = new();

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Companies.FirstOrDefault(company => company.Id == id));

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
        Task.FromResult(Transactions.FirstOrDefault(
            transaction => transaction.Id == id && transaction.Status != TransactionStatus.Eliminated));

    public Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(Guid employeeId, TransactionSource source, IReadOnlyCollection<string> externalReferences, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(Transactions
            .Where(transaction => transaction.EmployeeId == employeeId && transaction.Source == source && transaction.ExternalReference is not null && externalReferences.Contains(transaction.ExternalReference))
            .Select(transaction => transaction.ExternalReference!)
            .ToHashSet(StringComparer.Ordinal));

    public Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Transaction>>(Transactions
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status != TransactionStatus.Eliminated)
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
}

public sealed class FakeJobRunRepository : IJobRunRepository
{
    public List<JobRun> Runs { get; } = new();

    public void Add(JobRun run) => Runs.Add(run);

    public Task<JobRun?> FindLastAsync(string jobName, CancellationToken cancellationToken = default) =>
        Task.FromResult(MostRecentFirst(jobName).FirstOrDefault());

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
            .Where(investment => investment.Symbol is not null)
            .Select(investment => investment.EmployeeId)
            .Distinct()
            .ToList());

    public Task<IReadOnlyList<Investment>> ListTrackedByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Investment>>(Investments
            .Where(investment => investment.EmployeeId == employeeId && investment.Symbol is not null)
            .ToList());
}

public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    public MepQuote Quote { get; set; } = new(1544.30m, 1557.30m, new DateTimeOffset(2026, 9, 27, 14, 57, 0, TimeSpan.Zero), "DolarApi");

    public Exception? Failure { get; set; }

    public Task<MepQuote> GetMepQuoteAsync(CancellationToken cancellationToken = default) =>
        Failure is null ? Task.FromResult(Quote) : Task.FromException<MepQuote>(Failure);
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

    public string Source => "BYMA";

    public Task<IReadOnlyList<MarketPrice>> GetClosingPricesAsync(CancellationToken cancellationToken = default)
    {
        Calls++;

        return Failure is null
            ? Task.FromResult<IReadOnlyList<MarketPrice>>(Prices.ToList())
            : Task.FromException<IReadOnlyList<MarketPrice>>(Failure);
    }
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

    public void Complete(Guid employeeId, Lesson lesson) =>
        Completions.Add(LessonCompletion.Create(employeeId, lesson.Id, DateTimeOffset.UnixEpoch));

    public Task<IReadOnlyList<Course>> ListPublishedAsync(
        CourseLevel? level,
        int? maxDurationMinutes,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Course>>(Courses
            .Where(course => course.IsPublished)
            .Where(course => level is null || course.Level == level)
            .Where(course => maxDurationMinutes is null || course.DurationMinutes <= maxDurationMinutes)
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

    public void AddCompletion(LessonCompletion completion) => Completions.Add(completion);
}

public sealed class FakeArticleRepository : IArticleRepository
{
    public List<Article> Articles { get; } = new();

    public void Add(Article article) => Articles.Add(article);

    public Task<IReadOnlyList<Article>> ListPublishedAsync(
        EducationCategory? category,
        int? maxReadingTimeMinutes,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Article>>(Articles
            .Where(article => article.IsPublished)
            .Where(article => category is null || article.Category == category)
            .Where(article => maxReadingTimeMinutes is null || article.ReadingTimeMinutes <= maxReadingTimeMinutes)
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
