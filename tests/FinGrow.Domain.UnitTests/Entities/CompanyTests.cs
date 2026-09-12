namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class CompanyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_company_is_created_active()
    {
        CreateCompany().IsActive.ShouldBeTrue();
    }

    [Fact]
    public void A_department_is_added_to_the_company_aggregate()
    {
        var company = CreateCompany();

        var department = company.AddDepartment("Ventas", "Ventas y atencion al cliente", "Maria Gonzalez", Now);

        company.Departments.ShouldContain(department);
        department.CompanyId.ShouldBe(company.Id);
        department.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Two_departments_cannot_have_the_same_name()
    {
        var company = CreateCompany();
        company.AddDepartment("Ventas", null, null, Now);

        Should.Throw<DomainException>(() => company.AddDepartment("  ventas ", null, null, Now));
    }

    [Fact]
    public void A_department_is_deactivated_instead_of_deleted()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Logistica", null, null, Now);

        department.Deactivate(Now.AddDays(1));

        department.IsActive.ShouldBeFalse();
        company.Departments.ShouldContain(department);
    }

    [Fact]
    public void A_company_without_a_legal_name_is_not_created()
    {
        Should.Throw<DomainException>(() => Company.Create(
            "   ",
            TaxId.From("20123456786"),
            Email.From("hola@empresa.com"),
            "hash",
            Currency.ARS,
            Now));
    }

    private static Company CreateCompany() => Company.Create(
        "Empresa Demo S.A.",
        TaxId.From("20123456786"),
        Email.From("hola@empresa.com"),
        "hash-bcrypt",
        Currency.ARS,
        Now);
}
