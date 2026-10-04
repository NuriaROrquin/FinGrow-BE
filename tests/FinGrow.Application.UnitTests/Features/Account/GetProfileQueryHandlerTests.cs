namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Features.Account.Profile;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class GetProfileQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeCurrentUser _currentUser = new();

    private Company AddCompany()
    {
        var company = Company.Create(
            "Acme S.A.",
            TaxId.From("30712345671"),
            Email.From("rrhh@acme.com"),
            "hash",
            Currency.ARS,
            Now);

        _companies.Companies.Add(company);
        return company;
    }

    private Employee AddLoggedInEmployee(Company company, Guid? departmentId, string? phoneNumber)
    {
        var employee = Employee.Create(
            company.Id,
            departmentId,
            "Ana Gómez",
            Email.From("ana.gomez@acme.com"),
            phoneNumber,
            "hash",
            Currency.ARS,
            DateOnly.FromDateTime(Now.Date),
            Now);

        _employees.Employees.Add(employee);
        _currentUser.UserId = employee.Id;
        return employee;
    }

    private GetProfileQueryHandler CreateHandler() => new(_currentUser, _employees, _companies);

    [Fact]
    public async Task Returns_the_employee_data_with_company_and_department_names()
    {
        var company = AddCompany();
        var department = company.AddDepartment("Finanzas", description: null, managerName: null, Now);
        var employee = AddLoggedInEmployee(company, department.Id, "+54 11 1234-5678");
        employee.UpdateProfile("Ana Gómez", "+54 11 1234-5678", "30123456", new DateOnly(1990, 5, 20), "Av. Corrientes 1234", Now);

        var result = await CreateHandler().Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FullName.ShouldBe("Ana Gómez");
        result.Value.Email.ShouldBe("ana.gomez@acme.com");
        result.Value.PhoneNumber.ShouldBe("+54 11 1234-5678");
        result.Value.NationalId.ShouldBe("30123456");
        result.Value.BirthDate.ShouldBe(new DateOnly(1990, 5, 20));
        result.Value.Address.ShouldBe("Av. Corrientes 1234");
        result.Value.CompanyName.ShouldBe("Acme S.A.");
        result.Value.DepartmentName.ShouldBe("Finanzas");
    }

    [Fact]
    public async Task Department_is_empty_when_the_employee_has_none_assigned()
    {
        var company = AddCompany();
        company.AddDepartment("Finanzas", description: null, managerName: null, Now);
        AddLoggedInEmployee(company, departmentId: null, phoneNumber: null);

        var result = await CreateHandler().Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DepartmentName.ShouldBeNull();
        result.Value.PhoneNumber.ShouldBeNull();
        result.Value.NationalId.ShouldBeNull();
        result.Value.BirthDate.ShouldBeNull();
        result.Value.Address.ShouldBeNull();
    }

    [Fact]
    public async Task Requires_an_authenticated_user()
    {
        var result = await CreateHandler().Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.NoAutenticado");
    }
}
