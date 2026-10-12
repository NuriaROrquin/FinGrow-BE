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
        var managerId = Guid.CreateVersion7();

        var department = company.AddDepartment("Ventas", "Ventas y atencion al cliente", managerId, Now);

        company.Departments.ShouldContain(department);
        department.CompanyId.ShouldBe(company.Id);
        department.ManagerId.ShouldBe(managerId);
        department.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void A_department_can_be_left_without_a_manager()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Ventas", null, Guid.CreateVersion7(), Now);

        company.UpdateDepartment(department.Id, "Ventas", null, null, Now.AddDays(1));

        department.ManagerId.ShouldBeNull();
    }

    [Fact]
    public void An_empty_manager_id_is_rejected()
    {
        var company = CreateCompany();

        Should.Throw<DomainException>(() => company.AddDepartment("Ventas", null, Guid.Empty, Now));
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
        var managerId = Guid.CreateVersion7();

        company.UpdateDepartment(department.Id, "Ventas Online", "Canal digital", managerId, Now.AddDays(1));

        department.Name.ShouldBe("Ventas Online");
        department.Description.ShouldBe("Canal digital");
        department.ManagerId.ShouldBe(managerId);
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
    public void Deactivating_records_when_and_reactivating_clears_it()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Logistica", null, null, Now);

        company.DeactivateDepartment(department.Id, Now.AddDays(10));

        department.DeactivatedAt.ShouldBe(Now.AddDays(10));

        company.ActivateDepartment(department.Id, Now.AddDays(20));

        department.IsActive.ShouldBeTrue();
        department.DeactivatedAt.ShouldBeNull();
        department.UpdatedAt.ShouldBe(Now.AddDays(20));
    }

    [Fact]
    public void Deactivating_twice_keeps_the_first_date()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Logistica", null, null, Now);
        company.DeactivateDepartment(department.Id, Now.AddDays(10));

        company.DeactivateDepartment(department.Id, Now.AddDays(15));

        department.DeactivatedAt.ShouldBe(Now.AddDays(10));
    }

    [Fact]
    public void A_department_of_another_company_cannot_be_activated_or_deactivated()
    {
        var company = CreateCompany();
        var foreign = CreateCompany().AddDepartment("Ventas", null, null, Now);

        Should.Throw<DomainException>(() => company.DeactivateDepartment(foreign.Id, Now));
        Should.Throw<DomainException>(() => company.ActivateDepartment(foreign.Id, Now));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(31, true)]
    [InlineData(-1, false)]
    public void A_deactivated_department_was_active_during_the_periods_it_overlapped(int deactivatedDaysAfterStart, bool expected)
    {
        var periodStart = new DateTimeOffset(2026, 4, 1, 3, 0, 0, TimeSpan.Zero);
        var periodEnd = new DateTimeOffset(2026, 5, 1, 3, 0, 0, TimeSpan.Zero);
        var company = CreateCompany();
        var department = company.AddDepartment("Logistica", null, null, Now);
        company.DeactivateDepartment(department.Id, periodStart.AddDays(deactivatedDaysAfterStart));

        department.WasActiveDuring(periodStart, periodEnd).ShouldBe(expected);
    }

    [Fact]
    public void A_department_created_after_the_period_was_not_active_during_it()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Logistica", null, null, Now);

        department.WasActiveDuring(Now.AddDays(-30), Now).ShouldBeFalse();
        department.WasActiveDuring(Now.AddDays(-30), Now.AddTicks(1)).ShouldBeTrue();
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
