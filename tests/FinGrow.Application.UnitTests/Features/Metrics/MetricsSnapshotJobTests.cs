namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics.SnapshotMetrics;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class MetricsSnapshotJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 4, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompanyCreatedAt = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly MetricsPeriod July = MetricsPeriod.Of(2026, 7);
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);

    private readonly FakeCompanyRepository _companies = new();
    private readonly FakePeriodActivityReadRepository _activity = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task The_last_closed_month_gets_a_company_snapshot_and_one_per_active_department()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var tech = company.AddDepartment("Tecnologia", null, null, CompanyCreatedAt);
        var admin = company.AddDepartment("Administracion", null, null, CompanyCreatedAt);
        _companies.Companies.Add(company);
        _activity.Activity[(company.Id, August.Start)] = new List<EmployeePeriodActivity>
        {
            Employee(tech.Id, confirmedTransactions: 3, hasIntegration: true),
            Employee(tech.Id, hasBudget: true),
            Employee(departmentId: null, goalContributions: 1, hasActiveGoal: true, goalsAchieved: 1),
        };

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _snapshots.CompanySnapshots.Select(snapshot => snapshot.Period).ShouldBe(new[] { July, August });
        var companyAugust = _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == August);
        companyAugust.CompanyId.ShouldBe(company.Id);
        companyAugust.ComputedAt.ShouldBe(Now);
        companyAugust.Metrics.ShouldBe(PeriodMetrics.From(
            activeEmployees: 3,
            participatingEmployees: 2,
            confirmedTransactions: 3,
            employeesWithBudget: 1,
            employeesWithActiveGoal: 1,
            goalsAchieved: 1,
            employeesWithIntegration: 1));
        _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == July).Metrics.ShouldBe(PeriodMetrics.Empty);

        _snapshots.DepartmentSnapshots.Count.ShouldBe(4);
        var techAugust = _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.DepartmentId == tech.Id && snapshot.Period == August);
        techAugust.CompanyId.ShouldBe(company.Id);
        techAugust.Metrics.ShouldBe(PeriodMetrics.From(2, 1, 3, 1, 0, 0, 1));
        var adminAugust = _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.DepartmentId == admin.Id && snapshot.Period == August);
        adminAugust.Metrics.ShouldBe(PeriodMetrics.Empty);
        adminAugust.Metrics.ParticipationRate.ShouldBeNull();

        _unitOfWork.SaveCount.ShouldBe(1);
        result.Summary.ShouldBe("1 empresa(s) activa(s): 2 foto(s) de empresa y 4 de departamento para 2 periodo(s) (2026-07 a 2026-08).");
    }

    [Fact]
    public async Task Months_that_already_have_a_snapshot_are_skipped_but_the_last_closed_one_is_refreshed()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var tech = company.AddDepartment("Tecnologia", null, null, CompanyCreatedAt);
        _companies.Companies.Add(company);
        var earlier = Now.AddDays(-20);
        _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, July, PeriodMetrics.Empty, earlier));
        _snapshots.Add(DepartmentMetricsSnapshot.Take(company.Id, tech.Id, July, PeriodMetrics.Empty, earlier));
        _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, August, PeriodMetrics.Empty, earlier));
        _snapshots.Add(DepartmentMetricsSnapshot.Take(company.Id, tech.Id, August, PeriodMetrics.Empty, earlier));
        _activity.Activity[(company.Id, July.Start)] = new List<EmployeePeriodActivity> { Employee(tech.Id, confirmedTransactions: 9) };
        _activity.Activity[(company.Id, August.Start)] = new List<EmployeePeriodActivity> { Employee(tech.Id, confirmedTransactions: 2) };

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _activity.Requests.ShouldHaveSingleItem().Period.ShouldBe(August);
        _snapshots.CompanySnapshots.Count.ShouldBe(2);
        _snapshots.DepartmentSnapshots.Count.ShouldBe(2);

        var julyCompany = _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == July);
        julyCompany.Metrics.ShouldBe(PeriodMetrics.Empty);
        julyCompany.ComputedAt.ShouldBe(earlier);

        var augustCompany = _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == August);
        augustCompany.Metrics.ShouldBe(PeriodMetrics.From(1, 1, 2, 0, 0, 0, 0));
        augustCompany.ComputedAt.ShouldBe(Now);
        _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.Period == August).Metrics.ConfirmedTransactions.ShouldBe(2);
        result.Summary.ShouldBe("1 empresa(s) activa(s): 1 foto(s) de empresa y 1 de departamento para 1 periodo(s) (2026-08).");
    }

    [Fact]
    public async Task A_department_created_after_a_period_has_no_snapshot_for_that_period()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var tech = company.AddDepartment("Tecnologia", null, null, CompanyCreatedAt);
        var sales = company.AddDepartment("Ventas", null, null, new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        _companies.Companies.Add(company);

        await Job().ExecuteAsync(CancellationToken.None);

        _snapshots.DepartmentSnapshots.Where(snapshot => snapshot.DepartmentId == tech.Id).Select(snapshot => snapshot.Period).ShouldBe(new[] { July, August });
        _snapshots.DepartmentSnapshots.Where(snapshot => snapshot.DepartmentId == sales.Id).Select(snapshot => snapshot.Period).ShouldBe(new[] { August });
    }

    [Fact]
    public async Task Inactive_departments_are_not_photographed()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var closed = company.AddDepartment("Cerrado", null, null, CompanyCreatedAt);
        closed.Deactivate(Now.AddDays(-1));
        _companies.Companies.Add(company);

        await Job().ExecuteAsync(CancellationToken.None);

        _snapshots.CompanySnapshots.Count.ShouldBe(2);
        _snapshots.DepartmentSnapshots.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_company_created_this_month_has_nothing_to_photograph_yet()
    {
        _companies.Companies.Add(NewCompany("Nueva", new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero)));
        _clock.UtcNow = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _snapshots.CompanySnapshots.ShouldBeEmpty();
        _activity.Requests.ShouldBeEmpty();
        result.Summary.ShouldBe("1 empresa(s) activa(s); ninguna tenia un mes cerrado para fotografiar.");
    }

    [Fact]
    public async Task Inactive_companies_are_ignored()
    {
        var company = NewCompany("Baja", CompanyCreatedAt);
        company.Deactivate(Now.AddDays(-1));
        _companies.Companies.Add(company);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        _snapshots.CompanySnapshots.ShouldBeEmpty();
        result.Summary.ShouldBe("0 empresa(s) activa(s); ninguna tenia un mes cerrado para fotografiar.");
    }

    [Fact]
    public async Task The_job_is_registered_with_a_monthly_schedule()
    {
        var job = Job();

        job.Name.ShouldBe("metrics-snapshot");
        job.Schedule.ShouldBe("0 4 1 * *");
        job.Description.ShouldNotBeNullOrWhiteSpace();
    }

    private MetricsSnapshotJob Job() => new(
        _companies, _activity, _snapshots, _unitOfWork, _clock, NullLogger<MetricsSnapshotJob>.Instance);

    private static Company NewCompany(string name, DateTimeOffset createdAt) => Company.Create(
        name,
        TaxId.From("30712345671"),
        Email.From($"{name.ToLowerInvariant()}@empresa.com"),
        "hash",
        Currency.ARS,
        createdAt);

    private static EmployeePeriodActivity Employee(
        Guid? departmentId,
        int confirmedTransactions = 0,
        int goalContributions = 0,
        bool hasBudget = false,
        bool hasActiveGoal = false,
        int goalsAchieved = 0,
        bool hasIntegration = false) =>
        new(Guid.CreateVersion7(), departmentId, confirmedTransactions, goalContributions, hasBudget, hasActiveGoal, goalsAchieved, hasIntegration);
}
