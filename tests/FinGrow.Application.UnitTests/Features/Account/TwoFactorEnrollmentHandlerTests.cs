namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Features.Account.TwoFactor;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class TwoFactorEnrollmentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeTotpService _totp = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private Employee AddLoggedInEmployee()
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            departmentId: null,
            "Empleado Test",
            Email.From("empleado@empresa.com"),
            phoneNumber: null,
            "1234",
            Currency.ARS,
            DateOnly.FromDateTime(Now.Date),
            Now);

        _employees.Employees.Add(employee);
        _currentUser.UserId = employee.Id;
        return employee;
    }

    private readonly FakeCompanyRepository _companies = new();

    private TwoFactorAccountFinder CreateFinder() => new(_currentUser, _employees, _companies);

    private SetupTwoFactorCommandHandler CreateSetupHandler() =>
        new(CreateFinder(), _totp, _unitOfWork, new FakeDateTimeProvider(Now));

    private EnableTwoFactorCommandHandler CreateEnableHandler() =>
        new(CreateFinder(), _totp, _unitOfWork, new FakeDateTimeProvider(Now));

    private DisableTwoFactorCommandHandler CreateDisableHandler() =>
        new(CreateFinder(), _totp, _unitOfWork, new FakeDateTimeProvider(Now));

    private GetTwoFactorStatusQueryHandler CreateStatusHandler() => new(CreateFinder());

    private Company AddLoggedInCompany()
    {
        var company = Company.Create(
            "Empresa Demo S.A.",
            TaxId.From("20123456786"),
            Email.From("empresa@empresa.com"),
            "1234",
            Currency.ARS,
            Now);

        _companies.Companies.Add(company);
        _currentUser.UserId = company.Id;
        _currentUser.Role = "Empresa";
        return company;
    }

    [Fact]
    public async Task Status_is_disabled_until_the_enrollment_is_confirmed()
    {
        var employee = AddLoggedInEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);

        var pending = await CreateStatusHandler().Handle(new GetTwoFactorStatusQuery(), CancellationToken.None);
        employee.EnableTwoFactor(Now);
        var enabled = await CreateStatusHandler().Handle(new GetTwoFactorStatusQuery(), CancellationToken.None);

        pending.Value.Enabled.ShouldBeFalse();
        enabled.Value.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Status_requires_an_authenticated_user()
    {
        var result = await CreateStatusHandler().Handle(new GetTwoFactorStatusQuery(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.NoAutenticado");
    }

    [Fact]
    public async Task Setup_stores_a_new_secret_and_returns_the_provisioning_uri_without_enabling()
    {
        var employee = AddLoggedInEmployee();

        var result = await CreateSetupHandler().Handle(new SetupTwoFactorCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Secret.ShouldBe("SECRETBASE32");
        result.Value.ProvisioningUri.ShouldContain("empleado@empresa.com");
        employee.TwoFactorSecret.ShouldBe("SECRETBASE32");
        employee.IsTwoFactorEnabled.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Requesting_the_setup_again_replaces_the_pending_secret()
    {
        var employee = AddLoggedInEmployee();
        await CreateSetupHandler().Handle(new SetupTwoFactorCommand(), CancellationToken.None);

        _totp.NextSecret = "OTROSECRETO";
        await CreateSetupHandler().Handle(new SetupTwoFactorCommand(), CancellationToken.None);

        employee.TwoFactorSecret.ShouldBe("OTROSECRETO");
    }

    [Fact]
    public async Task Setup_is_rejected_when_two_factor_is_already_enabled()
    {
        var employee = AddLoggedInEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.EnableTwoFactor(Now);

        var result = await CreateSetupHandler().Handle(new SetupTwoFactorCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.DobleFactorYaActivo");
    }

    [Fact]
    public async Task Setup_requires_an_authenticated_user()
    {
        var result = await CreateSetupHandler().Handle(new SetupTwoFactorCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.NoAutenticado");
    }

    [Fact]
    public async Task Enable_with_a_valid_code_activates_two_factor()
    {
        var employee = AddLoggedInEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);

        var result = await CreateEnableHandler().Handle(new EnableTwoFactorCommand(FakeTotpService.ValidCode), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.IsTwoFactorEnabled.ShouldBeTrue();
        employee.TwoFactorEnabledAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Enable_with_an_invalid_code_leaves_two_factor_disabled()
    {
        var employee = AddLoggedInEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);

        var result = await CreateEnableHandler().Handle(new EnableTwoFactorCommand("000000"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.CodigoInvalido");
        employee.IsTwoFactorEnabled.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Enable_without_a_previous_setup_is_rejected()
    {
        AddLoggedInEmployee();

        var result = await CreateEnableHandler().Handle(new EnableTwoFactorCommand(FakeTotpService.ValidCode), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.DobleFactorSinAlta");
    }

    [Fact]
    public async Task Enable_is_rejected_when_two_factor_is_already_enabled()
    {
        var employee = AddLoggedInEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.EnableTwoFactor(Now);

        var result = await CreateEnableHandler().Handle(new EnableTwoFactorCommand(FakeTotpService.ValidCode), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.DobleFactorYaActivo");
    }

    [Fact]
    public async Task A_company_can_set_up_and_enable_two_factor()
    {
        var company = AddLoggedInCompany();

        var setup = await CreateSetupHandler().Handle(new SetupTwoFactorCommand(), CancellationToken.None);
        var enable = await CreateEnableHandler().Handle(new EnableTwoFactorCommand(FakeTotpService.ValidCode), CancellationToken.None);
        var status = await CreateStatusHandler().Handle(new GetTwoFactorStatusQuery(), CancellationToken.None);

        setup.IsSuccess.ShouldBeTrue();
        enable.IsSuccess.ShouldBeTrue();
        company.IsTwoFactorEnabled.ShouldBeTrue();
        status.Value.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Disable_with_a_valid_code_turns_two_factor_off()
    {
        var company = AddLoggedInCompany();
        company.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.EnableTwoFactor(Now);

        var result = await CreateDisableHandler().Handle(new DisableTwoFactorCommand(FakeTotpService.ValidCode), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        company.IsTwoFactorEnabled.ShouldBeFalse();
        company.TwoFactorSecret.ShouldBeNull();
    }

    [Fact]
    public async Task Disable_with_an_invalid_code_keeps_two_factor_on()
    {
        var employee = AddLoggedInEmployee();
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.EnableTwoFactor(Now);

        var result = await CreateDisableHandler().Handle(new DisableTwoFactorCommand("000000"), CancellationToken.None);

        result.Error.Code.ShouldBe("Account.CodigoInvalido");
        employee.IsTwoFactorEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Disable_is_rejected_when_two_factor_is_not_enabled()
    {
        AddLoggedInCompany();

        var result = await CreateDisableHandler().Handle(new DisableTwoFactorCommand(FakeTotpService.ValidCode), CancellationToken.None);

        result.Error.Code.ShouldBe("Account.DobleFactorNoActivo");
    }
}
