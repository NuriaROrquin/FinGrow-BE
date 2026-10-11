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
    public void A_department_can_be_renamed_and_described_again()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Ventas", null, null, Now);

        company.UpdateDepartment(department.Id, "Ventas Online", "Canal digital", "Maria Gonzalez", Now.AddDays(1));

        department.Name.ShouldBe("Ventas Online");
        department.Description.ShouldBe("Canal digital");
        department.ManagerName.ShouldBe("Maria Gonzalez");
        department.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void A_department_keeps_its_own_name_when_only_the_description_changes()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Ventas", null, null, Now);

        company.UpdateDepartment(department.Id, "VENTAS", "Otra descripcion", null, Now.AddDays(1));

        department.Name.ShouldBe("VENTAS");
    }

    [Fact]
    public void A_department_cannot_be_renamed_to_the_name_of_another_one()
    {
        var company = CreateCompany();
        company.AddDepartment("Ventas", null, null, Now);
        var marketing = company.AddDepartment("Marketing", null, null, Now);

        Should.Throw<DomainException>(() => company.UpdateDepartment(marketing.Id, " ventas", null, null, Now));
    }

    [Fact]
    public void A_removed_department_leaves_the_company()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Ventas", null, null, Now);

        company.RemoveDepartment(department.Id, Now.AddDays(1));

        company.Departments.ShouldBeEmpty();
        company.FindDepartment(department.Id).ShouldBeNull();
    }

    [Fact]
    public void A_department_of_another_company_cannot_be_updated_or_removed()
    {
        var company = CreateCompany();
        var foreign = CreateCompany().AddDepartment("Ventas", null, null, Now);

        Should.Throw<DomainException>(() => company.UpdateDepartment(foreign.Id, "Ventas", null, null, Now));
        Should.Throw<DomainException>(() => company.RemoveDepartment(foreign.Id, Now));
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
