namespace FinGrow.Application.UnitTests.Features.Departments.DeleteDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Departments.DeleteDepartment;

public class DeleteDepartmentCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private DeleteDepartmentCommandHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees, _scenario.UnitOfWork, _scenario.Clock);

    [Fact]
    public async Task An_empty_department_is_removed()
    {
        var sales = _scenario.AddDepartment("Ventas");

        var result = await CreateHandler().Handle(new DeleteDepartmentCommand(sales.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _scenario.Company.Departments.ShouldBeEmpty();
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_department_with_employees_is_rejected_with_conflict_saying_how_many_depend_on_it()
    {
        var sales = _scenario.AddDepartment("Ventas");
        _scenario.AddEmployee(sales);
        _scenario.AddEmployee(sales);
        _scenario.AddEmployee(sales);

        var result = await CreateHandler().Handle(new DeleteDepartmentCommand(sales.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Department.HasEmployees");
        result.Error.Description.ShouldContain("3 empleados");
        _scenario.Company.Departments.ShouldContain(sales);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_inactive_employee_still_blocks_the_removal()
    {
        var sales = _scenario.AddDepartment("Ventas");
        _scenario.AddEmployee(sales).Deactivate(CompanyStaffScenario.Now);

        var result = await CreateHandler().Handle(new DeleteDepartmentCommand(sales.Id), CancellationToken.None);

        result.Error.Description.ShouldContain("1 empleado asignado");
    }

    [Fact]
    public async Task An_unknown_department_is_not_found()
    {
        var result = await CreateHandler().Handle(new DeleteDepartmentCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
