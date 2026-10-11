namespace FinGrow.Application.UnitTests.Features.Departments.UpdateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Departments.UpdateDepartment;

public class UpdateDepartmentCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private UpdateDepartmentCommandHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _scenario.ManagerEligibility,
            _scenario.DepartmentResponses,
            _scenario.UnitOfWork,
            _scenario.Clock);

    [Fact]
    public async Task Saves_the_new_details_and_keeps_the_headcount()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var manager = _scenario.AddEmployee(sales, fullName: "Carlos Rodríguez");

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(sales.Id, "Ventas Online", "Canal digital", manager.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        sales.Name.ShouldBe("Ventas Online");
        sales.Description.ShouldBe("Canal digital");
        sales.ManagerId.ShouldBe(manager.Id);
        result.Value.Manager.ShouldNotBeNull().FullName.ShouldBe("Carlos Rodríguez");
        sales.UpdatedAt.ShouldBe(CompanyStaffScenario.Now);
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
    public async Task An_employee_of_another_company_cannot_be_the_manager()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        var outsider = _scenario.AddEmployee(department: null, company: other);

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(sales.Id, "Ventas", null, outsider.Id),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Department.ManagerNotAvailable");
        sales.ManagerId.ShouldBeNull();
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_manager_who_lost_access_can_stay_while_other_details_change()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var manager = _scenario.AddEmployee(sales);
        _scenario.Company.UpdateDepartment(sales.Id, "Ventas", null, manager.Id, CompanyStaffScenario.CreatedAt);
        manager.Deactivate(CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(sales.Id, "Ventas", "Nueva descripción", manager.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Manager.ShouldNotBeNull().IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task The_manager_can_be_removed()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var manager = _scenario.AddEmployee(sales);
        _scenario.Company.UpdateDepartment(sales.Id, "Ventas", null, manager.Id, CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new UpdateDepartmentCommand(sales.Id, "Ventas", null, null),
            CancellationToken.None);

        result.Value.Manager.ShouldBeNull();
        sales.ManagerId.ShouldBeNull();
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
