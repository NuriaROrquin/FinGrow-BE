namespace FinGrow.Application.UnitTests.Features.Departments;

using FinGrow.Application.Common;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal sealed class DepartmentsScenario
{
    public static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset Now = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    public DepartmentsScenario()
    {
        Company = Company.Create(
            "Acme S.A.",
            TaxId.From("20123456786"),
            Email.From("rrhh@acme.com"),
            "hash",
            Currency.ARS,
            CreatedAt);

        Companies.Companies.Add(Company);
        CurrentUser.UserId = Company.Id;
        CurrentUser.CompanyId = Company.Id;
        CurrentUser.Role = Rol.Empresa;
    }

    public Company Company { get; }

    public FakeCompanyRepository Companies { get; } = new();

    public FakeEmployeeRepository Employees { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public FakeDateTimeProvider Clock { get; } = new(Now);

    public Department AddDepartment(string name) => Company.AddDepartment(name, null, null, CreatedAt);

    public Employee AddEmployee(Department? department, Company? company = null)
    {
        var owner = company ?? Company;
        var employee = Employee.Create(
            owner.Id,
            department?.Id,
            "Ana Gomez",
            Email.From($"ana.{Guid.NewGuid():N}@acme.com"),
            null,
            "hash",
            Currency.ARS,
            DateOnly.FromDateTime(CreatedAt.Date),
            CreatedAt);

        Employees.Employees.Add(employee);
        return employee;
    }

    public void LogOut()
    {
        CurrentUser.UserId = null;
        CurrentUser.CompanyId = null;
        CurrentUser.Role = null;
    }
}
