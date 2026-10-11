namespace FinGrow.Application.UnitTests.Features.Departments.UpdateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Departments.UpdateDepartment;

public class UpdateDepartmentCommandHandlerTests
{
    private readonly DepartmentsScenario _scenario = new();

    private UpdateDepartmentCommandHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees, _scenario.UnitOfWork, _scenario.Clock);

    [Fact]
    public async Task Saves_the_new_details_and_keeps_the_headcount()
    {
        var sales = _scenario.AddDepartment("Ventas");
        _scenario.AddEmployee(sales);

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(sales.Id, "Ventas Online", "Canal digital", "Carlos Rodríguez"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        sales.Name.ShouldBe("Ventas Online");
        sales.Description.ShouldBe("Canal digital");
        sales.ManagerName.ShouldBe("Carlos Rodríguez");
        sales.UpdatedAt.ShouldBe(DepartmentsScenario.Now);
        result.Value.EmployeeCount.ShouldBe(1);
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Renaming_to_the_name_of_another_department_is_rejected_with_conflict()
    {
        _scenario.AddDepartment("Ventas");
        var marketing = _scenario.AddDepartment("Marketing");

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(marketing.Id, "ventas", null, null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        marketing.Name.ShouldBe("Marketing");
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_department_of_another_company_is_not_found()
    {
        var unknown = Guid.CreateVersion7();

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(unknown, "Ventas", null, null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Department.NotFound");
    }
}
