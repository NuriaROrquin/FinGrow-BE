namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics.GetParticipation;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;

public class GetParticipationQueryHandlerTests
{
    private static readonly DateOnly CurrentMonth = new(2026, 10, 1);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakePeriodActivityReadRepository _activity = new();

    private GetParticipationQueryHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Employees, _activity, _scenario.Clock);

    private void AddEmployees(int participating, int idle, int deactivated = 0)
    {
        var active = new List<EmployeePeriodActivity>();

        for (var index = 0; index < participating + idle; index++)
        {
            var employee = _scenario.AddEmployee(department: null);
            active.Add(new EmployeePeriodActivity(employee.Id, null, index < participating ? 3 : 0, 0, false, false, 0, false));
        }

        for (var index = 0; index < deactivated; index++)
        {
            _scenario.AddEmployee(department: null).Deactivate(CompanyStaffScenario.Now);
        }

        _activity.Activity[(_scenario.Company.Id, CurrentMonth)] = active;
    }

    [Fact]
    public async Task Counts_total_and_active_employees_and_derives_the_participation_rate_of_the_current_month()
    {
        AddEmployees(participating: 6, idle: 2, deactivated: 2);

        var result = await CreateHandler().Handle(new GetParticipationQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PeriodStart.ShouldBe(CurrentMonth);
        result.Value.PeriodEnd.ShouldBe(new DateOnly(2026, 10, 31));
        result.Value.TotalEmployees.ShouldBe(10);
        result.Value.ActiveEmployees.ShouldBe(8);
        result.Value.ParticipatingEmployees.ShouldBe(6);
        result.Value.ParticipationRate.ShouldBe(75m);
        result.Value.IsParticipationHidden.ShouldBeFalse();
    }

    [Fact]
    public async Task Hides_the_participation_when_the_active_group_is_too_small_to_stay_anonymous()
    {
        AddEmployees(participating: 1, idle: 3, deactivated: 4);

        var result = await CreateHandler().Handle(new GetParticipationQuery(), CancellationToken.None);

        result.Value.TotalEmployees.ShouldBe(8);
        result.Value.ActiveEmployees.ShouldBe(4);
        result.Value.ParticipatingEmployees.ShouldBeNull();
        result.Value.ParticipationRate.ShouldBeNull();
        result.Value.IsParticipationHidden.ShouldBeTrue();
        result.Value.MinimumGroupSize.ShouldBe(5);
    }

    [Fact]
    public async Task A_company_without_employees_has_no_rate_and_nothing_to_hide()
    {
        var result = await CreateHandler().Handle(new GetParticipationQuery(), CancellationToken.None);

        result.Value.TotalEmployees.ShouldBe(0);
        result.Value.ActiveEmployees.ShouldBe(0);
        result.Value.ParticipatingEmployees.ShouldBe(0);
        result.Value.ParticipationRate.ShouldBeNull();
        result.Value.IsParticipationHidden.ShouldBeFalse();
    }

    [Fact]
    public async Task Does_not_count_the_employees_of_another_company()
    {
        AddEmployees(participating: 5, idle: 0);
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        _scenario.AddEmployee(department: null, company: other);

        var result = await CreateHandler().Handle(new GetParticipationQuery(), CancellationToken.None);

        result.Value.TotalEmployees.ShouldBe(5);
        _activity.Requests.ShouldHaveSingleItem().CompanyId.ShouldBe(_scenario.Company.Id);
    }

    [Fact]
    public async Task Requires_an_authenticated_company()
    {
        _scenario.LogOut();

        var result = await CreateHandler().Handle(new GetParticipationQuery(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Metrics.NoAutenticado");
    }
}
