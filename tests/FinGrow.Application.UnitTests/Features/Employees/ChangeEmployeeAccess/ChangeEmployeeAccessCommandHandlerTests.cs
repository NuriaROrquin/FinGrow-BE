namespace FinGrow.Application.UnitTests.Features.Employees.ChangeEmployeeAccess;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Employees.ChangeEmployeeAccess;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;

public class ChangeEmployeeAccessCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();

    private ChangeEmployeeAccessCommandHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _scenario.Employees,
            _refreshTokens,
            _scenario.UnitOfWork,
            _scenario.Clock);

    private RefreshToken OpenSession(Employee employee)
    {
        var session = RefreshToken.Issue(employee.Id, _scenario.Company.Id, Rol.Empleado, $"token-{Guid.NewGuid():N}", CompanyStaffScenario.Now.AddMinutes(-5));
        _refreshTokens.Add(session);
        return session;
    }

    [Fact]
    public async Task Deactivating_keeps_the_employee_and_its_data_but_cuts_its_sessions()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var employee = _scenario.AddEmployee(sales);
        var session = OpenSession(employee);

        var result = await CreateHandler().Handle(new ChangeEmployeeAccessCommand(employee.Id, false), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
        result.Value.DepartmentName.ShouldBe("Ventas");
        employee.User.IsActive.ShouldBeFalse();
        _scenario.Employees.Employees.ShouldContain(employee);
        session.IsActive(CompanyStaffScenario.Now).ShouldBeFalse();
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Activating_gives_the_access_back()
    {
        var employee = _scenario.AddEmployee(department: null);
        employee.Deactivate(CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(new ChangeEmployeeAccessCommand(employee.Id, true), CancellationToken.None);

        result.Value.IsActive.ShouldBeTrue();
        employee.User.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Asking_for_the_current_state_changes_nothing()
    {
        var employee = _scenario.AddEmployee(department: null);
        var session = OpenSession(employee);

        var result = await CreateHandler().Handle(new ChangeEmployeeAccessCommand(employee.Id, true), CancellationToken.None);

        result.Value.IsActive.ShouldBeTrue();
        employee.UpdatedAt.ShouldBe(CompanyStaffScenario.CreatedAt);
        session.IsActive(CompanyStaffScenario.Now).ShouldBeTrue();
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_employee_of_another_company_is_not_found()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        var foreign = _scenario.AddEmployee(department: null, company: other);

        var result = await CreateHandler().Handle(new ChangeEmployeeAccessCommand(foreign.Id, false), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        foreign.User.IsActive.ShouldBeTrue();
    }
}
