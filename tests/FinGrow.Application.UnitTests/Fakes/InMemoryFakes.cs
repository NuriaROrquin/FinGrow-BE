namespace FinGrow.Application.UnitTests.Fakes;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

public sealed class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Employees { get; } = new();

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Employees.FirstOrDefault(employee => employee.Id == id));
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

    public bool IsAuthenticated => UserId is not null;
}
