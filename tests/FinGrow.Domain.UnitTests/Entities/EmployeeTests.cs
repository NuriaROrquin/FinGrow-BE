namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class EmployeeTests
{
    private static readonly Guid CompanyId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_employee_is_created_active_and_may_not_have_a_department_yet()
    {
        var employee = CreateEmployee();

        employee.IsActive.ShouldBeTrue();
        employee.DepartmentId.ShouldBeNull();
    }

    [Fact]
    public void An_employee_always_belongs_to_a_company()
    {
        Should.Throw<DomainException>(() => Employee.Create(
            Guid.Empty,
            departmentId: null,
            "Juan Perez",
            Email.From("juan@empresa.com"),
            null,
            "hash",
            Currency.ARS,
            new DateOnly(2026, 1, 15),
            Now));
    }

    [Fact]
    public void Reassigning_the_department_only_changes_the_id()
    {
        var employee = CreateEmployee();
        var departmentId = Guid.CreateVersion7();

        employee.AssignToDepartment(departmentId, Now.AddDays(1));

        employee.DepartmentId.ShouldBe(departmentId);
        employee.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void A_deactivated_employee_cannot_log_in()
    {
        var employee = CreateEmployee();
        employee.Deactivate(Now);

        Should.Throw<DomainException>(() => employee.RegisterLogin(Now.AddHours(1)));
    }

    [Fact]
    public void The_login_of_an_active_employee_is_recorded()
    {
        var employee = CreateEmployee();

        employee.RegisterLogin(Now.AddHours(1));

        employee.LastLoginAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Two_factor_is_disabled_until_the_enrollment_is_confirmed()
    {
        var employee = CreateEmployee();

        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);

        employee.TwoFactorSecret.ShouldBe("SECRETBASE32");
        employee.IsTwoFactorEnabled.ShouldBeFalse();

        employee.EnableTwoFactor(Now.AddMinutes(1));

        employee.IsTwoFactorEnabled.ShouldBeTrue();
        employee.TwoFactorEnabledAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public void Two_factor_cannot_be_enabled_without_starting_the_enrollment()
    {
        var employee = CreateEmployee();

        Should.Throw<DomainException>(() => employee.EnableTwoFactor(Now));
    }

    [Fact]
    public void An_enabled_two_factor_cannot_be_enabled_or_reenrolled_again()
    {
        var employee = CreateEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.EnableTwoFactor(Now);

        Should.Throw<DomainException>(() => employee.EnableTwoFactor(Now));
        Should.Throw<DomainException>(() => employee.StartTwoFactorEnrollment("OTROSECRETO", Now));
        employee.TwoFactorSecret.ShouldBe("SECRETBASE32");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void The_two_factor_secret_is_required(string secret)
    {
        var employee = CreateEmployee();

        Should.Throw<DomainException>(() => employee.StartTwoFactorEnrollment(secret, Now));
    }

    private static Employee CreateEmployee() => Employee.Create(
        CompanyId,
        departmentId: null,
        "Juan Perez",
        Email.From("juan.perez@empresa.com"),
        "+54 9 11 1234-5678",
        "hash-bcrypt",
        Currency.ARS,
        new DateOnly(2026, 1, 15),
        Now);
}
