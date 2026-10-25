namespace FinGrow.Api.UnitTests.CompanyStaff;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;

public class CompanyMetricsEndpointsTests
{
    private static readonly Uri ParticipationPath = new("/api/company/metrics/participation", UriKind.Relative);

    private static readonly Uri TrendsPath = new("/api/company/metrics/trends", UriKind.Relative);

    private static readonly Uri ReportPath = new("/api/company/metrics/report", UriKind.Relative);

    private static void AddActiveEmployees(CompanyStaffWebApplicationFactory factory, int participating, int idle)
    {
        for (var index = 0; index < participating + idle; index++)
        {
            var employee = factory.AddEmployee(department: null, $"empleado{index}@acme.com");
            factory.Activity.Add(new EmployeePeriodActivity(employee.Id, null, index < participating ? 2 : 0, 0, false, false, 0, false));
        }
    }

    [Fact]
    public async Task Participation_returns_the_totals_and_the_rate_of_the_company_of_the_token()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        AddActiveEmployees(factory, participating: 3, idle: 2);
        factory.AddEmployee(department: null, "sin.acceso@acme.com").Deactivate(CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.GetAsync(ParticipationPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"totalEmployees\":6");
        body.ShouldContain("\"activeEmployees\":5");
        body.ShouldContain("\"participatingEmployees\":3");
        body.ShouldContain("\"participationRate\":60");
        body.ShouldContain("\"isParticipationHidden\":false");
    }

    [Fact]
    public async Task Participation_never_carries_data_of_a_particular_employee()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        AddActiveEmployees(factory, participating: 3, idle: 2);
        var client = factory.CreateCompanyClient();

        var body = await (await client.GetAsync(ParticipationPath)).Content.ReadAsStringAsync();

        body.ShouldNotContain("Ana Gomez");
        body.ShouldNotContain("@acme.com");
        factory.Employees.ShouldAllBe(employee => !body.Contains(employee.Id.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task A_small_group_gets_its_participation_hidden()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        AddActiveEmployees(factory, participating: 1, idle: 1);
        var client = factory.CreateCompanyClient();

        var body = await (await client.GetAsync(ParticipationPath)).Content.ReadAsStringAsync();

        body.ShouldContain("\"activeEmployees\":2");
        body.ShouldContain("\"participatingEmployees\":null");
        body.ShouldContain("\"participationRate\":null");
        body.ShouldContain("\"isParticipationHidden\":true");
    }

    [Fact]
    public async Task An_employee_token_is_rejected_with_403()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateClientFor(Rol.Empleado);

        var response = await client.GetAsync(ParticipationPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Trends_reject_an_employee_token_with_403()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateClientFor(Rol.Empleado);

        var response = await client.GetAsync(TrendsPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_monthly_report_rejects_an_employee_token_with_403()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateClientFor(Rol.Empleado);

        var response = await client.GetAsync(ReportPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
