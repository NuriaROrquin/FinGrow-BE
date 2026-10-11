namespace FinGrow.Application.UnitTests.Features.Employees.ListEmployees;

using FinGrow.Application.Features.Employees.ListEmployees;

public class ListEmployeesQueryHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private ListEmployeesQueryHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees);

    [Fact]
    public async Task Lists_only_the_employees_of_the_company_with_their_department_name()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var seller = _scenario.AddEmployee(sales);
        var unassigned = _scenario.AddEmployee(department: null);
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        _scenario.AddEmployee(department: null, company: other);

        var result = await CreateHandler().Handle(new ListEmployeesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.Single(employee => employee.Id == seller.Id).DepartmentName.ShouldBe("Ventas");
        result.Value.Single(employee => employee.Id == unassigned.Id).DepartmentName.ShouldBeNull();
    }

    [Fact]
    public async Task Requires_an_authenticated_company()
    {
        _scenario.LogOut();

        var result = await CreateHandler().Handle(new ListEmployeesQuery(), CancellationToken.None);

        result.Error.Code.ShouldBe("Employee.NoAutenticado");
    }
}
