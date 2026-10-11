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
    public void An_employee_created_in_a_department_starts_its_assignment_history()
    {
        var departmentId = Guid.CreateVersion7();

        var employee = Employee.Create(
            CompanyId,
            departmentId,
            "Juan Perez",
            Email.From("juan@empresa.com"),
            null,
            "hash",
            Currency.ARS,
            new DateOnly(2026, 1, 15),
            Now);

        var assignment = employee.DepartmentAssignments.ShouldHaveSingleItem();
        assignment.DepartmentId.ShouldBe(departmentId);
        assignment.StartedAt.ShouldBe(Now);
        assignment.IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public void An_employee_without_a_department_has_no_assignment_history()
    {
        CreateEmployee().DepartmentAssignments.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_the_department_closes_the_previous_assignment_and_opens_a_new_one()
    {
        var employee = CreateEmployee();
        var sales = Guid.CreateVersion7();
        var marketing = Guid.CreateVersion7();
        employee.AssignToDepartment(sales, Now.AddDays(1));

        employee.AssignToDepartment(marketing, Now.AddDays(30));

        employee.DepartmentAssignments.Count.ShouldBe(2);
        var previous = employee.DepartmentAssignments.Single(assignment => assignment.DepartmentId == sales);
        previous.StartedAt.ShouldBe(Now.AddDays(1));
        previous.EndedAt.ShouldBe(Now.AddDays(30));
        var current = employee.DepartmentAssignments.Single(assignment => assignment.IsCurrent);
        current.DepartmentId.ShouldBe(marketing);
        current.StartedAt.ShouldBe(Now.AddDays(30));
    }

    [Fact]
    public void Leaving_every_department_only_closes_the_current_assignment()
    {
        var employee = CreateEmployee();
        employee.AssignToDepartment(Guid.CreateVersion7(), Now.AddDays(1));

        employee.AssignToDepartment(null, Now.AddDays(2));

        employee.DepartmentId.ShouldBeNull();
        employee.DepartmentAssignments.ShouldHaveSingleItem().EndedAt.ShouldBe(Now.AddDays(2));
    }

    [Fact]
    public void Assigning_the_same_department_again_does_not_touch_the_history()
    {
        var employee = CreateEmployee();
        var sales = Guid.CreateVersion7();
        employee.AssignToDepartment(sales, Now.AddDays(1));

        employee.AssignToDepartment(sales, Now.AddDays(5));

        employee.DepartmentAssignments.ShouldHaveSingleItem().IsCurrent.ShouldBeTrue();
        employee.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void An_assignment_cannot_end_before_it_started()
    {
        var employee = CreateEmployee();
        employee.AssignToDepartment(Guid.CreateVersion7(), Now.AddDays(10));

        Should.Throw<DomainException>(() => employee.AssignToDepartment(Guid.CreateVersion7(), Now));
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

    [Fact]
    public void A_new_employee_starts_with_the_default_preferences()
    {
        var employee = CreateEmployee();

        employee.Theme.ShouldBe(Theme.System);
        employee.Language.ShouldBe(Language.es);
        employee.DateFormat.ShouldBe(DateFormat.DayMonthYear);
        employee.PreferredCurrency.ShouldBe(Currency.ARS);
    }

    [Fact]
    public void Updating_the_preferences_saves_the_four_of_them()
    {
        var employee = CreateEmployee();

        employee.UpdatePreferences(Theme.Dark, Language.en, Currency.USD, DateFormat.YearMonthDay, Now.AddDays(1));

        employee.Theme.ShouldBe(Theme.Dark);
        employee.Language.ShouldBe(Language.en);
        employee.PreferredCurrency.ShouldBe(Currency.USD);
        employee.DateFormat.ShouldBe(DateFormat.YearMonthDay);
        employee.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void A_preference_outside_the_enum_is_rejected()
    {
        var employee = CreateEmployee();

        Should.Throw<DomainException>(() => employee.UpdatePreferences(
            (Theme)99,
            Language.es,
            Currency.ARS,
            DateFormat.DayMonthYear,
            Now));
        employee.Theme.ShouldBe(Theme.System);
    }

    [Fact]
    public void The_company_can_correct_the_name_email_and_phone()
    {
        var employee = CreateEmployee();

        employee.UpdateDetails(" Juan Pérez ", Email.From("jperez@empresa.com"), null, Now.AddDays(1));

        employee.FullName.ShouldBe("Juan Pérez");
        employee.Email.Value.ShouldBe("jperez@empresa.com");
        employee.PhoneNumber.ShouldBeNull();
        employee.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void Correcting_the_details_keeps_the_personal_data_of_the_employee()
    {
        var employee = CreateEmployee();
        employee.UpdateProfile("Juan Perez", null, "30123456", new DateOnly(1990, 5, 20), "Av. Siempre Viva 742", Now);

        employee.UpdateDetails("Juan Perez", Email.From("juan.perez@empresa.com"), null, Now.AddDays(1));

        employee.NationalId.ShouldBe("30123456");
        employee.BirthDate.ShouldBe(new DateOnly(1990, 5, 20));
        employee.Address.ShouldBe("Av. Siempre Viva 742");
    }

    [Fact]
    public void Correcting_the_details_validates_the_phone_like_the_profile_does()
    {
        var employee = CreateEmployee();

        Should.Throw<DomainException>(() =>
            employee.UpdateDetails("Juan Perez", Email.From("juan.perez@empresa.com"), "hola", Now));
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
