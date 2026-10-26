namespace FinGrow.Api.UnitTests.CompanyStaff;

using System.Net.Http.Headers;
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

internal sealed class CompanyStaffWebApplicationFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    public CompanyStaffWebApplicationFactory()
    {
        Company = Company.Create(
            "Acme S.A.",
            TaxId.From("20123456786"),
            Email.From("rrhh@acme.com"),
            "hash",
            Currency.ARS,
            CreatedAt);
        Companies.Add(Company);
    }

    public Company Company { get; }

    public List<Company> Companies { get; } = new();

    public List<Employee> Employees { get; } = new();

    public Employee AddEmployee(Department? department, string email = "ana.gomez@acme.com")
    {
        var employee = Employee.Create(
            Company.Id,
            department?.Id,
            "Ana Gomez",
            Email.From(email),
            null,
            "hash",
            Currency.ARS,
            DateOnly.FromDateTime(CreatedAt.Date),
            CreatedAt);

        Employees.Add(employee);
        return employee;
    }

    public HttpClient CreateClientFor(string role)
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(Company.Id, Company.Id, role, Company.Name, Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    public HttpClient CreateCompanyClient() => CreateClientFor(Rol.Empresa);

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
                ["Email:ResendApiKey"] = "re_unit_test_key",
                ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                ["Jobs:ApiKey"] = "unit-test-jobs-api-key",
            }));

        builder.ConfigureTestServices(services =>
        {
            services.TreatEverySessionAsOpen();
            services.AddScoped<ICompanyRepository>(_ => new InMemoryCompanyRepository(Companies));
            services.AddScoped<IEmployeeRepository>(_ => new InMemoryEmployeeRepository(Employees));
            services.AddScoped<IUserRepository>(_ => new InMemoryUserRepository(Companies, Employees));
            services.AddScoped<IOutboundEmailRepository>(_ => new InMemoryOutboundEmailRepository(QueuedEmails));
            services.AddScoped<ITransactionReceiptRepository, NoReceiptsRepository>();
            services.AddScoped<INotificationRepository>(_ => new RecordingNotificationRepository(ClearedRecipients));
            services.AddScoped<IRefreshTokenRepository>(_ => new RecordingRefreshTokenRepository(RevokedUsers));
            services.AddScoped<IPeriodActivityReadRepository>(_ => new InMemoryPeriodActivityReadRepository(Activity));
            services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        });
    }

    public List<EmployeePeriodActivity> Activity { get; } = new();

    public List<NotificationRecipient> ClearedRecipients { get; } = new();

    public List<Guid> RevokedUsers { get; } = new();

    public List<OutboundEmail> QueuedEmails { get; } = new();

    private sealed class InMemoryOutboundEmailRepository(List<OutboundEmail> emails) : IOutboundEmailRepository
    {
        public void Add(OutboundEmail email) => emails.Add(email);

        public Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(emails.FirstOrDefault(email => email.Id == id));

        public Task<IReadOnlyList<Guid>> ListDueIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }

    private sealed class RecordingRefreshTokenRepository(List<Guid> revokedUsers) : IRefreshTokenRepository
    {
        public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

        public void Add(RefreshToken refreshToken)
        {
        }

        public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            revokedUsers.Add(userId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RefreshToken>>(Array.Empty<RefreshToken>());

        public Task<RefreshToken?> FindActiveBySessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

        public Task RevokeAllForUserExceptSessionAsync(
            Guid userId,
            Guid keptSessionId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoReceiptsRepository : ITransactionReceiptRepository
    {
        public void Add(TransactionReceipt receipt) => throw new NotSupportedException();

        public void Remove(TransactionReceipt receipt) => throw new NotSupportedException();

        public Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TransactionReceipt?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, Guid>> GetIdsByTransactionAsync(
            IReadOnlyCollection<Guid> transactionIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListStorageKeysByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<IReadOnlyList<TransactionReceipt>> ListDetachedUploadedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingNotificationRepository(List<NotificationRecipient> cleared) : INotificationRepository
    {
        public void Add(Notification notification) => throw new NotSupportedException();

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsSinceAsync(
            NotificationRecipient recipient,
            NotificationType type,
            string deduplicationKey,
            DateTimeOffset since,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Notification>> ListAsync(
            NotificationRecipient recipient,
            bool unreadOnly,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> CountAsync(NotificationRecipient recipient, bool unreadOnly, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Notification>> ListUnreadAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RemoveAllForAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default)
        {
            cleared.Add(recipient);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCompanyRepository(List<Company> companies) : ICompanyRepository
    {
        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(companies.FirstOrDefault(company => company.Id == id));

        public Task<Company?> GetWithDepartmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<Company>> ListActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Company>>(companies.Where(company => company.User.IsActive).ToList());
    }

    private sealed class InMemoryUserRepository(List<Company> companies, List<Employee> employees) : IUserRepository
    {
        private IEnumerable<User> Users =>
            companies.Select(company => company.User).Concat(employees.Select(employee => employee.User));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(user => user.Id == id));

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(user => user.Email == email));

        public void Remove(User user)
        {
            companies.RemoveAll(company => company.Id == user.Id);
            employees.RemoveAll(employee => employee.Id == user.Id);
        }
    }

    private sealed class InMemoryEmployeeRepository(List<Employee> employees) : IEmployeeRepository
    {
        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(employees.FirstOrDefault(employee => employee.Id == id));

        public Task<IReadOnlyDictionary<Guid, int>> CountByDepartmentAsync(
            Guid companyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(employees
                .Where(employee => employee.CompanyId == companyId && employee.DepartmentId is not null)
                .GroupBy(employee => employee.DepartmentId!.Value)
                .ToDictionary(group => group.Key, group => group.Count()));

        public Task<DateTimeOffset?> FindLastWithdrawalAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(employees.Where(employee => employee.CompanyId == companyId).Max(employee => employee.WithdrawnAt));

        public Task<IReadOnlyList<Employee>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Employee>>(employees
                .Where(employee => employee.CompanyId == companyId)
                .OrderBy(employee => employee.FullName, StringComparer.Ordinal)
                .ToList());

        public Task<IReadOnlyList<Employee>> ListByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Employee>>(employees.Where(employee => employee.DepartmentId == departmentId).ToList());

        public Task<IReadOnlyList<Employee>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Employee>>(employees.Where(employee => ids.Contains(employee.Id)).ToList());

        public void Add(Employee employee) => employees.Add(employee);

        public void Remove(Employee employee) => employees.Remove(employee);
    }

    private sealed class InMemoryPeriodActivityReadRepository(List<EmployeePeriodActivity> activity) : IPeriodActivityReadRepository
    {
        public Task<IReadOnlyList<EmployeePeriodActivity>> ListByCompanyAsync(
            Guid companyId,
            MetricsPeriod period,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeePeriodActivity>>(activity);
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
