namespace FinGrow.Application.UnitTests.Features.Employees.UpdateEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Employees.UpdateEmployee;

public class UpdateEmployeeCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private UpdateEmployeeCommandHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _scenario.Employees, _scenario.Users, _scenario.UnitOfWork, _scenario.Clock);

    [Fact]
    public async Task Saves_the_new_name_email_and_phone()
    {
        var sales = _scenario.AddDepartment("Ventas");
        var employee = _scenario.AddEmployee(sales, email: "ana@acme.com");

        var result = await CreateHandler().Handle(
            new UpdateEmployeeCommand(employee.Id, "Ana María Gómez", "amgomez@acme.com", "+54 11 4000-0000"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.FullName.ShouldBe("Ana María Gómez");
        employee.User.Email.Value.ShouldBe("amgomez@acme.com");
        employee.PhoneNumber.ShouldBe("+54 11 4000-0000");
        employee.DepartmentId.ShouldBe(sales.Id);
        result.Value.DepartmentName.ShouldBe("Ventas");
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Keeping_the_same_email_is_not_a_conflict()
    {
        var employee = _scenario.AddEmployee(department: null, email: "ana@acme.com");

        var result = await CreateHandler().Handle(
            new UpdateEmployeeCommand(employee.Id, "Ana Gómez", "ANA@acme.com", null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task The_email_of_another_account_is_rejected_with_conflict()
    {
        _scenario.AddEmployee(department: null, email: "bruno@acme.com");
        var employee = _scenario.AddEmployee(department: null, email: "ana@acme.com");

        var result = await CreateHandler().Handle(
            new UpdateEmployeeCommand(employee.Id, "Ana Gómez", "bruno@acme.com", null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        employee.User.Email.Value.ShouldBe("ana@acme.com");
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_employee_of_another_company_is_not_found()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com");
        var foreign = _scenario.AddEmployee(department: null, company: other, email: "ana@otra.com");

        var result = await CreateHandler().Handle(
            new UpdateEmployeeCommand(foreign.Id, "Hackeado", "ana@otra.com", null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        foreign.FullName.ShouldBe("Ana Gomez");
    }
}
