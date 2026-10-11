namespace FinGrow.Application.UnitTests.Features.Employees.ChangeEmployeeDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Employees.ChangeEmployeeDepartment;

public class ChangeEmployeeDepartmentCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private ChangeEmployeeDepartmentCommandHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees, _scenario.UnitOfWork, _scenario.Clock);

    [Fact]
    public async Task Moves_the_employee_and_keeps_the_previous_department_in_the_history()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var marketing = _scenario.AddDepartment("Marketing");
        var employee = _scenario.AddEmployee(sales);

        var result = await CreateHandler().Handle(
            new ChangeEmployeeDepartmentCommand(employee.Id, marketing.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DepartmentName.ShouldBe("Marketing");
        employee.DepartmentId.ShouldBe(marketing.Id);
        var previous = employee.DepartmentAssignments.Single(assignment => assignment.DepartmentId == sales.Id);
        previous.EndedAt.ShouldBe(CompanyStaffScenario.Now);
        employee.DepartmentAssignments.Single(assignment => assignment.IsCurrent).DepartmentId.ShouldBe(marketing.Id);
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_employee_can_be_left_without_a_department()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var employee = _scenario.AddEmployee(sales);

        var result = await CreateHandler().Handle(
            new ChangeEmployeeDepartmentCommand(employee.Id, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DepartmentName.ShouldBeNull();
        employee.DepartmentId.ShouldBeNull();
    }

    [Fact]
    public async Task An_inactive_department_is_rejected()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var closed = _scenario.AddDepartment("Logística");
        closed.Deactivate(CompanyStaffScenario.Now);
        var employee = _scenario.AddEmployee(sales);

        var result = await CreateHandler().Handle(
            new ChangeEmployeeDepartmentCommand(employee.Id, closed.Id),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Employee.DepartmentNotAvailable");
        employee.DepartmentId.ShouldBe(sales.Id);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_department_of_another_company_is_rejected()
    {
        var employee = _scenario.AddEmployee(department: null);
        var foreign = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com")
            .AddDepartment("Ventas", null, null, CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new ChangeEmployeeDepartmentCommand(employee.Id, foreign.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        employee.DepartmentId.ShouldBeNull();
    }

    [Fact]
    public async Task An_employee_of_another_company_is_not_found()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        var foreign = _scenario.AddEmployee(department: null, company: other);

        var result = await CreateHandler().Handle(
            new ChangeEmployeeDepartmentCommand(foreign.Id, sales.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        foreign.DepartmentId.ShouldBeNull();
    }
}
