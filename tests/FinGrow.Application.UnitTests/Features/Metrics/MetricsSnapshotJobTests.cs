namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics.FinancialHealth;
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
    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakePeriodActivityReadRepository _activity = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();
    private readonly FakeFinancialHealthReadRepository _finances = new();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeFinancialHealthScoreRepository _scores = new();
    private readonly FakeExchangeRateProvider _exchangeRates = new();
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
            employeesWithIntegration: 1,
            employeesWithScore: 0,
            financialHealthScoreSum: 0));
        _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == July).Metrics.ShouldBe(PeriodMetrics.Empty);

        _snapshots.DepartmentSnapshots.Count.ShouldBe(4);
        var techAugust = _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.DepartmentId == tech.Id && snapshot.Period == August);
        techAugust.CompanyId.ShouldBe(company.Id);
        techAugust.Metrics.ShouldBe(PeriodMetrics.From(2, 1, 3, 1, 0, 0, 1, 0, 0));
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
        augustCompany.Metrics.ShouldBe(PeriodMetrics.From(1, 1, 2, 0, 0, 0, 0, 0, 0));
        augustCompany.ComputedAt.ShouldBe(Now);
        _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.Period == August).Metrics.ConfirmedTransactions.ShouldBe(2);
        result.Summary.ShouldBe("1 empresa(s) activa(s): 1 foto(s) de empresa y 1 de departamento para 1 periodo(s) (2026-08).");
    }

    [Fact]
    public async Task A_withdrawal_after_the_last_closed_month_keeps_its_existing_snapshot()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var tech = company.AddDepartment("Tecnologia", null, null, CompanyCreatedAt);
        _companies.Companies.Add(company);
        var earlier = Now.AddDays(-20);
        var taken = PeriodMetrics.From(2, 2, 7, 0, 0, 0, 0, 0, 0);
        _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, July, PeriodMetrics.Empty, earlier));
        _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, August, taken, earlier));
        _snapshots.Add(DepartmentMetricsSnapshot.Take(company.Id, tech.Id, August, taken, earlier));
        _employees.Employees.Add(WithdrawnEmployee(company.Id, withdrawnAt: August.EndInstantExclusive.AddHours(1)));
        _activity.Activity[(company.Id, August.Start)] = new List<EmployeePeriodActivity> { Employee(tech.Id, confirmedTransactions: 2) };

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _activity.Requests.ShouldBeEmpty();
        var august = _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == August);
        august.Metrics.ShouldBe(taken);
        august.ComputedAt.ShouldBe(earlier);
        _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.Period == August).Metrics.ShouldBe(taken);
    }

    [Fact]
    public async Task A_withdrawal_during_the_last_closed_month_does_not_stop_its_refresh()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        _companies.Companies.Add(company);
        _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, July, PeriodMetrics.Empty, Now.AddDays(-20)));
        _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, August, PeriodMetrics.Empty, Now.AddDays(-20)));
        _employees.Employees.Add(WithdrawnEmployee(company.Id, withdrawnAt: August.StartInstant.AddDays(3)));
        _activity.Activity[(company.Id, August.Start)] = new List<EmployeePeriodActivity> { Employee(null, confirmedTransactions: 2) };

        await Job().ExecuteAsync(CancellationToken.None);

        var august = _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == August);
        august.ComputedAt.ShouldBe(Now);
        august.Metrics.ConfirmedTransactions.ShouldBe(2);
    }

    [Fact]
    public async Task A_withdrawal_does_not_stop_a_missing_snapshot_from_being_taken()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        _companies.Companies.Add(company);
        _employees.Employees.Add(WithdrawnEmployee(company.Id, withdrawnAt: Now.AddDays(-1)));

        await Job().ExecuteAsync(CancellationToken.None);

        _snapshots.CompanySnapshots.Select(snapshot => snapshot.Period).ShouldBe(new[] { July, August });
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
    public async Task A_department_is_not_photographed_for_the_months_after_it_was_deactivated()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var closed = company.AddDepartment("Cerrado", null, null, CompanyCreatedAt);
        closed.Deactivate(new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero));
        _companies.Companies.Add(company);

        await Job().ExecuteAsync(CancellationToken.None);

        _snapshots.CompanySnapshots.Count.ShouldBe(2);
        _snapshots.DepartmentSnapshots.ShouldHaveSingleItem().Period.ShouldBe(July);
    }

    [Fact]
    public async Task A_department_deactivated_after_the_month_closed_keeps_its_photo_of_that_month()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var closed = company.AddDepartment("Cerrado", null, null, CompanyCreatedAt);
        closed.Deactivate(Now.AddDays(-1));
        _companies.Companies.Add(company);

        await Job().ExecuteAsync(CancellationToken.None);

        _snapshots.DepartmentSnapshots.Select(snapshot => snapshot.Period).ShouldBe(new[] { July, August });
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
    public async Task Snapshots_add_up_the_scores_of_the_employees_scored_that_month()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var tech = company.AddDepartment("Tecnologia", null, null, CompanyCreatedAt);
        _companies.Companies.Add(company);
        var saver = Employee(tech.Id, confirmedTransactions: 12);
        var spender = Employee(tech.Id, confirmedTransactions: 15);
        var newcomer = Employee(departmentId: null, confirmedTransactions: 2);
        _activity.Activity[(company.Id, August.Start)] = new List<EmployeePeriodActivity> { saver, spender, newcomer };
        _finances.Totals.Add(Pesos(saver.EmployeeId, monthIncome: 100000m, monthSpending: 80000m));
        _finances.Totals.Add(Pesos(spender.EmployeeId, monthIncome: 100000m, monthSpending: 120000m));
        _finances.Totals.Add(Pesos(newcomer.EmployeeId, monthIncome: 100000m, monthSpending: 10000m));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _scores.Scores.Select(score => score.EmployeeId).ShouldBe(new[] { saver.EmployeeId, spender.EmployeeId }, ignoreOrder: true);
        var companyAugust = _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == August).Metrics;
        companyAugust.EmployeesWithScore.ShouldBe(2);
        companyAugust.FinancialHealthScoreSum.ShouldBe(100 + 27);
        companyAugust.AverageFinancialHealthScore.ShouldBe(63.5m);
        var techAugust = _snapshots.DepartmentSnapshots.Single(snapshot => snapshot.Period == August).Metrics;
        techAugust.EmployeesWithScore.ShouldBe(2);
        techAugust.FinancialHealthScoreSum.ShouldBe(127);
    }

    [Fact]
    public async Task A_month_that_needs_the_mep_rate_and_cannot_get_it_stays_pending_for_the_next_run()
    {
        var company = NewCompany("Acme", CompanyCreatedAt);
        var tech = company.AddDepartment("Tecnologia", null, null, CompanyCreatedAt);
        _companies.Companies.Add(company);
        var freelancer = Employee(tech.Id, confirmedTransactions: 12);
        _activity.Activity[(company.Id, August.Start)] = new List<EmployeePeriodActivity> { freelancer };
        _finances.Totals.Add(new EmployeeCurrencyTotals(freelancer.EmployeeId, Currency.USD, new DateOnly(2026, 8, 3), 2000m, 1500m, 2000m, 1500m, 1500m));
        _exchangeRates.Failure = new HttpRequestException("DolarApi no responde");

        var failed = await Job().ExecuteAsync(CancellationToken.None);

        failed.IsFailure.ShouldBeTrue();
        failed.Error.ShouldBe("No se pudo obtener la cotizacion del dolar MEP: 1 foto(s) de empresa quedaron pendientes para la proxima corrida.");
        failed.Summary.ShouldBe("1 empresa(s) activa(s): 1 foto(s) de empresa y 1 de departamento para 1 periodo(s) (2026-07).");
        _snapshots.CompanySnapshots.ShouldHaveSingleItem().Period.ShouldBe(July);
        _scores.Scores.ShouldBeEmpty();

        _exchangeRates.Failure = null;
        var retried = await Job().ExecuteAsync(CancellationToken.None);

        retried.IsSuccess.ShouldBeTrue();
        _snapshots.CompanySnapshots.Select(snapshot => snapshot.Period).ShouldBe(new[] { July, August });
        _snapshots.CompanySnapshots.Single(snapshot => snapshot.Period == August).Metrics.EmployeesWithScore.ShouldBe(1);
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
        _companies,
        _employees,
        _activity,
        _snapshots,
        new FinancialHealthScorer(_finances, _goals, _scores, _exchangeRates, NullLogger<FinancialHealthScorer>.Instance),
        _unitOfWork,
        _clock,
        NullLogger<MetricsSnapshotJob>.Instance);

    private static EmployeeCurrencyTotals Pesos(Guid employeeId, decimal monthIncome, decimal monthSpending) =>
        new(employeeId, Currency.ARS, new DateOnly(2026, 5, 10), monthIncome, monthSpending, 1000000m, 700000m, 300000m);

    private static FinGrow.Domain.Entities.Employee WithdrawnEmployee(Guid companyId, DateTimeOffset withdrawnAt)
    {
        var employee = FinGrow.Domain.Entities.Employee.Create(
            companyId,
            departmentId: null,
            "Ana Gómez",
            Email.From("ana.gomez@acme.com"),
            phoneNumber: null,
            "hash",
            Currency.ARS,
            new DateOnly(2026, 7, 15),
            CompanyCreatedAt);
        employee.Withdraw(withdrawnAt);
        return employee;
    }

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
