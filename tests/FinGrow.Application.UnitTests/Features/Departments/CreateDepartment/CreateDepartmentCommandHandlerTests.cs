namespace FinGrow.Application.UnitTests.Features.Departments.CreateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Departments.CreateDepartment;

public class CreateDepartmentCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private CreateDepartmentCommandHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _scenario.ManagerEligibility,
            _scenario.DepartmentResponses,
            _scenario.UnitOfWork,
            _scenario.Clock);

    private static bool IsValid(CreateDepartmentCommand command) =>
        new CreateDepartmentCommandValidator().Validate(command).IsValid;

    [Fact]
    public async Task Creates_the_department_inside_the_authenticated_company()
    {
        var manager = _scenario.AddEmployee(department: null, fullName: "María González");

        var result = await CreateHandler().Handle(
            new CreateDepartmentCommand(" Ventas ", "Ventas y atención al cliente", manager.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var department = _scenario.Company.Departments.ShouldHaveSingleItem();
        department.Name.ShouldBe("Ventas");
        department.Description.ShouldBe("Ventas y atención al cliente");
        department.ManagerId.ShouldBe(manager.Id);
        result.Value.Manager.ShouldNotBeNull().FullName.ShouldBe("María González");
        department.CompanyId.ShouldBe(_scenario.Company.Id);
        department.CreatedAt.ShouldBe(CompanyStaffScenario.Now);
        result.Value.Id.ShouldBe(department.Id);
        result.Value.EmployeeCount.ShouldBe(0);
        result.Value.IsActive.ShouldBeTrue();
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_repeated_name_is_rejected_with_conflict()
    {
        _scenario.AddDepartment("Ventas");

        var result = await CreateHandler().Handle(new CreateDepartmentCommand("VENTAS", null, null), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Department.NameTaken");
        _scenario.Company.Departments.Count.ShouldBe(1);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Requires_an_authenticated_company()
    {
        _scenario.LogOut();

        var result = await CreateHandler().Handle(new CreateDepartmentCommand("Ventas", null, null), CancellationToken.None);

        result.Error.Code.ShouldBe("Department.NoAutenticado");
        _scenario.Company.Departments.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_requires_a_name(string name) =>
        IsValid(new CreateDepartmentCommand(name, null, null)).ShouldBeFalse();

    [Fact]
    public void Validator_rejects_texts_longer_than_the_columns()
    {
        IsValid(new CreateDepartmentCommand(new string('a', 121), null, null)).ShouldBeFalse();
        IsValid(new CreateDepartmentCommand("Ventas", new string('a', 501), null)).ShouldBeFalse();
        IsValid(new CreateDepartmentCommand("Ventas", null, Guid.Empty)).ShouldBeFalse();
    }

    [Fact]
    public async Task An_employee_of_another_company_cannot_be_the_manager()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        var outsider = _scenario.AddEmployee(department: null, company: other);

        var result = await CreateHandler().Handle(
            new CreateDepartmentCommand("Ventas", null, outsider.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("Department.ManagerNotAvailable");
        _scenario.Company.Departments.ShouldBeEmpty();
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_employee_without_access_cannot_be_the_manager()
    {
        var inactive = _scenario.AddEmployee(department: null);
        inactive.Deactivate(CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new CreateDepartmentCommand("Ventas", null, inactive.Id),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Department.ManagerNotAvailable");
    }

    [Fact]
    public async Task An_unknown_employee_cannot_be_the_manager()
    {
        var result = await CreateHandler().Handle(
            new CreateDepartmentCommand("Ventas", null, Guid.CreateVersion7()),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Department.ManagerNotAvailable");
    }

    [Fact]
    public void Validator_accepts_a_department_with_only_a_name() =>
        IsValid(new CreateDepartmentCommand("Ventas", null, null)).ShouldBeTrue();
}
