namespace FinGrow.Application.UnitTests.Features.PasswordReset;

using FinGrow.Application.Common;
using FinGrow.Application.Features.PasswordReset;
using FinGrow.Application.Features.PasswordReset.RequestPasswordReset;
using FinGrow.Application.Features.PasswordReset.ResetPassword;
using FinGrow.Application.Features.PasswordReset.ValidateToken;
using FinGrow.Application.Features.Session;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class PasswordResetHandlersTests
{
    private const string ResetUrl = "http://localhost:3000/recuperar-password/restablecer";

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakePasswordResetTokenRepository _tokens = new();
    private readonly FakeOutboundEmailRepository _outboundEmails = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    private FakeUserRepository Users => new(_employees, _companies);

    private Employee AddEmployee(string email = "ana@empresa.com")
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            departmentId: null,
            "Ana Pérez",
            Email.From(email),
            phoneNumber: null,
            "vieja-1234",
            Currency.ARS,
            DateOnly.FromDateTime(Now.Date),
            Now);

        _employees.Employees.Add(employee);
        return employee;
    }

    private Company AddCompany()
    {
        var company = Company.Create(
            "Empresa Demo S.A.",
            TaxId.From("20123456786"),
            Email.From("empresa@empresa.com"),
            "vieja-1234",
            Currency.ARS,
            Now);

        _companies.Companies.Add(company);
        return company;
    }

    private string IssueToken(Guid userId, DateTimeOffset? createdAt = null)
    {
        var value = PasswordResetToken.GenerateValue();
        _tokens.Add(PasswordResetToken.Issue(userId, value, createdAt ?? Now));
        return value;
    }

    private RequestPasswordResetCommandHandler RequestHandler() =>
        new(
            Users,
            new SessionAccountResolver(_employees, _companies),
            _tokens,
            _outboundEmails,
            _unitOfWork,
            _clock,
            Options.Create(new PasswordResetOptions { ResetUrl = ResetUrl }));

    private ResetPasswordCommandHandler ResetHandler() =>
        new(_tokens, Users, new FakePasswordHasher(), _refreshTokens, _unitOfWork, _clock);

    private ValidatePasswordResetTokenQueryHandler ValidateHandler() =>
        new(_tokens, Users, _clock);

    [Fact]
    public async Task Requesting_a_link_queues_an_email_with_a_single_use_token()
    {
        var employee = AddEmployee();

        var result = await RequestHandler().Handle(
            new RequestPasswordResetCommand("ana@empresa.com"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var token = _tokens.Tokens.ShouldHaveSingleItem();
        token.UserId.ShouldBe(employee.Id);
        token.ExpiresAt.ShouldBe(Now + PasswordResetToken.Lifetime);

        var email = _outboundEmails.Emails.ShouldHaveSingleItem();
        email.Recipient.ShouldBe(employee.User.Email);
        email.TextBody!.ShouldContain("Hola Ana Pérez,");
        email.TextBody!.ShouldContain($"{ResetUrl}?token=");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_link_in_the_email_carries_the_value_whose_hash_is_stored()
    {
        AddEmployee();

        await RequestHandler().Handle(new RequestPasswordResetCommand("ana@empresa.com"), CancellationToken.None);

        var body = _outboundEmails.Emails.Single().TextBody!;
        var start = body.IndexOf("?token=", StringComparison.Ordinal) + "?token=".Length;
        var value = body[start..].Split('\n')[0].Trim();
        PasswordResetToken.Hash(value).ShouldBe(_tokens.Tokens.Single().TokenHash);
    }

    [Theory]
    [InlineData("nadie@empresa.com")]
    [InlineData("no-es-un-email")]
    public async Task An_unknown_email_gets_the_same_answer_and_no_email(string email)
    {
        AddEmployee();

        var result = await RequestHandler().Handle(new RequestPasswordResetCommand(email), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _tokens.Tokens.ShouldBeEmpty();
        _outboundEmails.Emails.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_deactivated_employee_gets_no_email()
    {
        AddEmployee().Deactivate(Now);

        var result = await RequestHandler().Handle(
            new RequestPasswordResetCommand("ana@empresa.com"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _outboundEmails.Emails.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_company_is_found_by_its_email_without_asking_for_the_role()
    {
        AddEmployee();
        var company = AddCompany();

        await RequestHandler().Handle(new RequestPasswordResetCommand("empresa@empresa.com"), CancellationToken.None);

        var token = _tokens.Tokens.ShouldHaveSingleItem();
        token.UserId.ShouldBe(company.Id);
        var email = _outboundEmails.Emails.ShouldHaveSingleItem();
        email.Recipient.ShouldBe(company.User.Email);
        email.TextBody!.ShouldContain("Hola Empresa Demo S.A.,");
    }

    [Fact]
    public async Task A_deactivated_company_gets_no_email()
    {
        AddCompany().Deactivate(Now);

        await RequestHandler().Handle(new RequestPasswordResetCommand("empresa@empresa.com"), CancellationToken.None);

        _tokens.Tokens.ShouldBeEmpty();
        _outboundEmails.Emails.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_second_request_within_the_resend_interval_sends_nothing_new()
    {
        AddEmployee();
        await RequestHandler().Handle(new RequestPasswordResetCommand("ana@empresa.com"), CancellationToken.None);

        _clock.UtcNow = Now + TimeSpan.FromSeconds(30);
        await RequestHandler().Handle(new RequestPasswordResetCommand("ana@empresa.com"), CancellationToken.None);

        _outboundEmails.Emails.Count.ShouldBe(1);

        _clock.UtcNow = Now + RequestPasswordResetCommandHandler.ResendInterval;
        await RequestHandler().Handle(new RequestPasswordResetCommand("ana@empresa.com"), CancellationToken.None);

        _outboundEmails.Emails.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Resetting_changes_the_password_burns_every_link_and_closes_the_sessions()
    {
        var employee = AddEmployee();
        var older = IssueToken(employee.Id, Now - TimeSpan.FromMinutes(10));
        var value = IssueToken(employee.Id);
        var session = RefreshToken.Issue(employee.Id, employee.CompanyId, Rol.Empleado, RefreshToken.GenerateValue(), Now);
        _refreshTokens.Add(session);

        var result = await ResetHandler().Handle(new ResetPasswordCommand(value, "nueva-5678"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.User.PasswordHash.ShouldBe("nueva-5678");
        _tokens.Tokens.ShouldAllBe(token => token.UsedAt == Now);
        session.RevokedAt.ShouldBe(Now);

        var reuse = await ResetHandler().Handle(new ResetPasswordCommand(older, "otra-9999"), CancellationToken.None);
        reuse.Error.ShouldBe(PasswordResetErrors.EnlaceInvalido);
        employee.User.PasswordHash.ShouldBe("nueva-5678");
    }

    [Fact]
    public async Task Resetting_keeps_two_factor_enabled()
    {
        var employee = AddEmployee();
        employee.User.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.User.EnableTwoFactor(Now);
        var value = IssueToken(employee.Id);

        await ResetHandler().Handle(new ResetPasswordCommand(value, "nueva-5678"), CancellationToken.None);

        employee.User.IsTwoFactorEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task A_company_resets_its_password_with_the_same_link_flow()
    {
        var company = AddCompany();
        var value = IssueToken(company.Id);

        var result = await ResetHandler().Handle(new ResetPasswordCommand(value, "nueva-5678"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        company.User.PasswordHash.ShouldBe("nueva-5678");
    }

    [Fact]
    public async Task An_expired_link_changes_nothing()
    {
        var employee = AddEmployee();
        var value = IssueToken(employee.Id);
        _clock.UtcNow = Now + PasswordResetToken.Lifetime;

        var result = await ResetHandler().Handle(new ResetPasswordCommand(value, "nueva-5678"), CancellationToken.None);

        result.Error.ShouldBe(PasswordResetErrors.EnlaceInvalido);
        employee.User.PasswordHash.ShouldBe("vieja-1234");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_link_of_an_account_deactivated_after_asking_no_longer_works()
    {
        var employee = AddEmployee();
        var value = IssueToken(employee.Id);
        employee.Deactivate(Now);

        var validation = await ValidateHandler().Handle(new ValidatePasswordResetTokenQuery(value), CancellationToken.None);
        var reset = await ResetHandler().Handle(new ResetPasswordCommand(value, "nueva-5678"), CancellationToken.None);

        validation.Error.ShouldBe(PasswordResetErrors.EnlaceInvalido);
        reset.Error.ShouldBe(PasswordResetErrors.EnlaceInvalido);
        employee.User.PasswordHash.ShouldBe("vieja-1234");
    }

    [Fact]
    public async Task Validating_a_usable_link_does_not_use_it()
    {
        var employee = AddEmployee();
        var value = IssueToken(employee.Id);

        var result = await ValidateHandler().Handle(new ValidatePasswordResetTokenQuery(value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _tokens.Tokens.Single().UsedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("token-que-no-existe")]
    public async Task Validating_an_unknown_link_fails(string value)
    {
        var result = await ValidateHandler().Handle(new ValidatePasswordResetTokenQuery(value), CancellationToken.None);

        result.Error.ShouldBe(PasswordResetErrors.EnlaceInvalido);
    }

    [Fact]
    public void The_reset_command_does_not_print_its_secrets()
    {
        var text = new ResetPasswordCommand("token-secreto", "clave-secreta").ToString();

        text.ShouldNotContain("token-secreto");
        text.ShouldNotContain("clave-secreta");
    }
}
