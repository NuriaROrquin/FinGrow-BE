namespace FinGrow.Application.UnitTests.Features.Departments.ListDepartments;

using FinGrow.Application.Features.Departments.ListDepartments;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class ListDepartmentsQueryHandlerTests
{
    private readonly DepartmentsScenario _scenario = new();

    private ListDepartmentsQueryHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees);

    [Fact]
    public async Task Lists_the_departments_of_the_company_sorted_by_name_with_their_headcount()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var it = _scenario.AddDepartment("IT");
        _scenario.AddEmployee(sales);
        _scenario.AddEmployee(sales);
        _scenario.AddEmployee(department: null);

        var result = await CreateHandler().Handle(new ListDepartmentsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value[0].Name.ShouldBe("IT");
        result.Value[1].Name.ShouldBe("Ventas");
        result.Value.Single(department => department.Id == sales.Id).EmployeeCount.ShouldBe(2);
        result.Value.Single(department => department.Id == it.Id).EmployeeCount.ShouldBe(0);
    }

    [Fact]
    public async Task Does_not_show_the_departments_of_another_company()
    {
        _scenario.AddDepartment("Ventas");
        var other = Company.Create(
            "Otra S.A.",
            TaxId.From("20123456786"),
            Email.From("hola@otra.com"),
            "hash",
            Currency.ARS,
            DepartmentsScenario.CreatedAt);
        other.AddDepartment("Finanzas", null, null, DepartmentsScenario.CreatedAt);
        _scenario.Companies.Companies.Add(other);

        var result = await CreateHandler().Handle(new ListDepartmentsQuery(), CancellationToken.None);

        result.Value.ShouldHaveSingleItem().Name.ShouldBe("Ventas");
    }

    [Fact]
    public async Task Requires_an_authenticated_company()
    {
        _scenario.LogOut();

        var result = await CreateHandler().Handle(new ListDepartmentsQuery(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Department.NoAutenticado");
    }
}
