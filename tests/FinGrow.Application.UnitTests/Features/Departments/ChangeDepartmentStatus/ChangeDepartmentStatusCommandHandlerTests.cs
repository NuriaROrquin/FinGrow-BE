namespace FinGrow.Application.UnitTests.Features.Departments.ChangeDepartmentStatus;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Departments.ChangeDepartmentStatus;

public class ChangeDepartmentStatusCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private ChangeDepartmentStatusCommandHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees, _scenario.UnitOfWork, _scenario.Clock);

    [Fact]
    public async Task Deactivating_an_empty_department_keeps_it_in_the_company()
    {
        var logistics = _scenario.AddDepartment("Logística");

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(logistics.Id, false, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
        logistics.IsActive.ShouldBeFalse();
        logistics.DeactivatedAt.ShouldBe(CompanyStaffScenario.Now);
        _scenario.Company.Departments.ShouldContain(logistics);
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Deactivating_moves_its_employees_to_the_chosen_department_and_keeps_their_history()
    {
        var marketing = _scenario.AddDepartment("Marketing");
        var sales = _scenario.AddDepartment("Ventas");
        var ana = _scenario.AddEmployee(marketing);
        var bruno = _scenario.AddEmployee(marketing);
        var carla = _scenario.AddEmployee(sales);

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(marketing.Id, false, sales.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmployeeCount.ShouldBe(0);
        ana.DepartmentId.ShouldBe(sales.Id);
        bruno.DepartmentId.ShouldBe(sales.Id);
        carla.DepartmentId.ShouldBe(sales.Id);
        ana.DepartmentAssignments.Single(assignment => assignment.DepartmentId == marketing.Id)
            .EndedAt.ShouldBe(CompanyStaffScenario.Now);
        marketing.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Deactivating_without_a_target_leaves_its_employees_without_department()
    {
        var marketing = _scenario.AddDepartment("Marketing");
        var ana = _scenario.AddEmployee(marketing);

        await CreateHandler().Handle(new ChangeDepartmentStatusCommand(marketing.Id, false, null), CancellationToken.None);

        ana.DepartmentId.ShouldBeNull();
        ana.DepartmentAssignments.ShouldHaveSingleItem().IsCurrent.ShouldBeFalse();
    }

    [Fact]
    public async Task Moving_the_employees_to_an_inactive_department_is_rejected()
    {
        var marketing = _scenario.AddDepartment("Marketing");
        var closed = _scenario.AddDepartment("Cerrado");
        closed.Deactivate(CompanyStaffScenario.CreatedAt);
        var ana = _scenario.AddEmployee(marketing);

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(marketing.Id, false, closed.Id),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Department.InvalidMoveTarget");
        marketing.IsActive.ShouldBeTrue();
        ana.DepartmentId.ShouldBe(marketing.Id);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Moving_the_employees_to_the_same_department_is_rejected()
    {
        var marketing = _scenario.AddDepartment("Marketing");

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(marketing.Id, false, marketing.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        marketing.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Moving_the_employees_to_a_department_of_another_company_is_rejected()
    {
        var marketing = _scenario.AddDepartment("Marketing");
        var foreign = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com")
            .AddDepartment("Ventas", null, null, CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(marketing.Id, false, foreign.Id),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Department.InvalidMoveTarget");
    }

    [Fact]
    public async Task Reactivating_makes_it_available_again()
    {
        var logistics = _scenario.AddDepartment("Logística");
        logistics.Deactivate(CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(logistics.Id, true, null),
            CancellationToken.None);

        result.Value.IsActive.ShouldBeTrue();
        logistics.DeactivatedAt.ShouldBeNull();
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Asking_for_the_current_state_changes_nothing()
    {
        var marketing = _scenario.AddDepartment("Marketing");
        var ana = _scenario.AddEmployee(marketing);

        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(marketing.Id, true, null),
            CancellationToken.None);

        result.Value.EmployeeCount.ShouldBe(1);
        ana.DepartmentId.ShouldBe(marketing.Id);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_department_of_another_company_is_not_found()
    {
        var result = await CreateHandler().Handle(
            new ChangeDepartmentStatusCommand(Guid.CreateVersion7(), false, null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
