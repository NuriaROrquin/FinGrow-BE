namespace FinGrow.Application.UnitTests.Features.Departments.ListDepartments;

using FinGrow.Application.Features.Departments.ListDepartments;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class ListDepartmentsQueryHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private ListDepartmentsQueryHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.DepartmentResponses);

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
    public async Task Shows_the_name_of_each_manager()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var manager = _scenario.AddEmployee(sales, fullName: "María González");
        _scenario.Company.UpdateDepartment(sales.Id, "Ventas", null, manager.Id, CompanyStaffScenario.Now);
        _scenario.AddDepartment("IT");

        var result = await CreateHandler().Handle(new ListDepartmentsQuery(), CancellationToken.None);

        var managed = result.Value.Single(department => department.Id == sales.Id).Manager.ShouldNotBeNull();
        managed.Id.ShouldBe(manager.Id);
        managed.FullName.ShouldBe("María González");
        managed.IsActive.ShouldBeTrue();
        result.Value.Single(department => department.Name == "IT").Manager.ShouldBeNull();
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
            CompanyStaffScenario.CreatedAt);
        other.AddDepartment("Finanzas", null, null, CompanyStaffScenario.CreatedAt);
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
