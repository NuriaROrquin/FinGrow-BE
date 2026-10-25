namespace FinGrow.Application.UnitTests.Fakes;

using FinGrow.Domain.Repositories;

public sealed class FakeNotificationAudienceRepository : INotificationAudienceRepository
{
    public List<Guid> EmployeesWithInvestments { get; } = new();

    public List<Guid> ActiveEmployees { get; } = new();

    public Task<IReadOnlyList<Guid>> ListEmployeesWithInvestmentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(EmployeesWithInvestments.ToList());

    public Task<IReadOnlyList<Guid>> ListActiveEmployeeIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(ActiveEmployees.ToList());
}
