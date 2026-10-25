namespace FinGrow.Application.UnitTests.Features.Employees.CreateEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Employees.CreateEmployee;
using FinGrow.Domain.Enums;

public class CreateEmployeeCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private CreateEmployeeCommandHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _scenario.Employees,
            _scenario.Users,
            _scenario.OutboundEmails,
            _scenario.PasswordHasher,
            _scenario.UnitOfWork,
            _scenario.Clock);

    private static bool IsValid(CreateEmployeeCommand command) =>
        new CreateEmployeeCommandValidator().Validate(command).IsValid;

    [Fact]
    public async Task Creates_the_employee_in_the_company_with_a_temporary_password()
    {
        var sales = _scenario.AddDepartment("Ventas");

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand(" Juan Pérez ", "Juan.Perez@Acme.com", "+54 11 1234-5678", sales.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var employee = _scenario.Employees.Employees.ShouldHaveSingleItem();
        employee.CompanyId.ShouldBe(_scenario.Company.Id);
        employee.DepartmentId.ShouldBe(sales.Id);
        employee.FullName.ShouldBe("Juan Pérez");
        employee.User.Email.Value.ShouldBe("juan.perez@acme.com");
        employee.User.IsActive.ShouldBeTrue();
        employee.HiredOn.ShouldBe(new DateOnly(2026, 10, 11));
        employee.PreferredCurrency.ShouldBe(Currency.ARS);
        employee.User.PasswordHash.ShouldBe(result.Value.TemporaryPassword);
        result.Value.Employee.Id.ShouldBe(employee.Id);
        result.Value.Employee.DepartmentName.ShouldBe("Ventas");
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_temporary_password_mixes_letters_and_digits_and_changes_every_time()
    {
        var first = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", null, null),
            CancellationToken.None);
        var second = await CreateHandler().Handle(
            new CreateEmployeeCommand("Bruno Díaz", "bruno@acme.com", null, null),
            CancellationToken.None);

        var password = first.Value.TemporaryPassword;
        password.Length.ShouldBe(TemporaryPassword.Length);
        password.ShouldContain(character => char.IsAsciiLetterUpper(character));
        password.ShouldContain(character => char.IsAsciiLetterLower(character));
        password.ShouldContain(character => char.IsAsciiDigit(character));
        second.Value.TemporaryPassword.ShouldNotBe(password);
    }

    [Fact]
    public async Task An_employee_can_be_created_without_a_department()
    {
        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", null, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Employee.DepartmentId.ShouldBeNull();
        result.Value.Employee.DepartmentName.ShouldBeNull();
    }

    [Fact]
    public async Task An_email_that_already_exists_is_rejected_with_conflict()
    {
        _scenario.AddEmployee(department: null, email: "ana@acme.com");

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ANA@acme.com", null, null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Employee.EmailTaken");
        _scenario.Employees.Employees.Count.ShouldBe(1);
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_email_used_by_a_company_is_rejected_with_conflict()
    {
        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "rrhh@acme.com", null, null),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Employee.EmailTaken");
        _scenario.Employees.Employees.ShouldBeEmpty();
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_department_of_another_company_is_rejected()
    {
        var foreign = CompanyStaffScenario.CreateCompany("Otra S.A.", "hola@otra.com")
            .AddDepartment("Ventas", null, null, CompanyStaffScenario.CreatedAt);

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", null, foreign.Id),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Employee.DepartmentNotAvailable");
        _scenario.Employees.Employees.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_inactive_department_is_rejected()
    {
        var sales = _scenario.AddDepartment("Ventas");
        sales.Deactivate(CompanyStaffScenario.Now);

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", null, sales.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        _scenario.Employees.Employees.ShouldBeEmpty();
    }

    [Fact]
    public async Task Requires_an_authenticated_company()
    {
        _scenario.LogOut();

        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", null, null),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Employee.NoAutenticado");
    }

    [Theory]
    [InlineData("", "ana@acme.com", null)]
    [InlineData("A", "ana@acme.com", null)]
    [InlineData("Ana Gómez", "", null)]
    [InlineData("Ana Gómez", "ana@acme", null)]
    [InlineData("Ana Gómez", "ana@acme.com", "hola")]
    public void Validator_rejects_invalid_details(string fullName, string email, string? phoneNumber) =>
        IsValid(new CreateEmployeeCommand(fullName, email, phoneNumber, null)).ShouldBeFalse();

    [Fact]
    public void Validator_accepts_an_employee_without_phone_or_department() =>
        IsValid(new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", "  ", null)).ShouldBeTrue();

    [Fact]
    public async Task The_welcome_email_with_the_temporary_password_is_queued_together_with_the_employee()
    {
        var result = await CreateHandler().Handle(
            new CreateEmployeeCommand("Juan Pérez", "juan.perez@acme.com", null, null),
            CancellationToken.None);

        var email = _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
        email.Recipient.Value.ShouldBe("juan.perez@acme.com");
        email.Subject.ShouldBe(WelcomeEmail.Subject);
        email.Status.ShouldBe(OutboundEmailStatus.Pending);
        email.TextBody.ShouldNotBeNull();
        email.TextBody.ShouldContain("Juan Pérez");
        email.TextBody.ShouldContain("Acme S.A.");
        email.TextBody.ShouldContain("juan.perez@acme.com");
        email.TextBody.ShouldContain(result.Value.TemporaryPassword);
        email.HtmlBody.ShouldNotBeNull();
        email.HtmlBody.ShouldContain(result.Value.TemporaryPassword);
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_html_of_the_welcome_email_escapes_the_employee_name()
    {
        await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana <b>Gómez</b>", "ana@acme.com", null, null),
            CancellationToken.None);

        var email = _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
        email.HtmlBody.ShouldNotBeNull();
        email.HtmlBody.ShouldContain("Ana &lt;b&gt;");
        email.HtmlBody.ShouldNotContain("<b>Gómez</b>");
    }

    [Fact]
    public async Task A_rejected_registration_queues_no_email()
    {
        _scenario.AddEmployee(department: null, email: "ana@acme.com");

        await CreateHandler().Handle(
            new CreateEmployeeCommand("Ana Gómez", "ana@acme.com", null, null),
            CancellationToken.None);

        _scenario.OutboundEmails.Emails.ShouldBeEmpty();
    }
}
