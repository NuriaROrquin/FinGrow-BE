namespace FinGrow.Application.UnitTests.Features.Login;

using FinGrow.Application.Features.Login;
using FinGrow.Application.Features.Session;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class LoginCompanyCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private static Company CreateCompany(string plainPassword = "1234", bool isActive = true)
    {
        var company = Company.Create(
            "Empresa Demo S.A.",
            TaxId.From("20123456786"),
            Email.From("empresa@empresa.com"),
            plainPassword,
            Currency.ARS,
            DateTimeOffset.UtcNow);

        if (!isActive)
        {
            company.Deactivate(DateTimeOffset.UtcNow);
        }

        return company;
    }

    private static LoginCompanyCommandHandler CreateHandler(Company? company = null)
    {
        var companyRepository = new FakeCompanyRepository();

        if (company is not null)
        {
            companyRepository.Companies.Add(company);
        }

        var tokenService = new FakeTokenService();

        return new LoginCompanyCommandHandler(
            companyRepository,
            new FakePasswordHasher(),
            new SessionIssuer(tokenService, new FakeRefreshTokenRepository(), new FakeDateTimeProvider(Now)),
            tokenService,
            new FakeUnitOfWork());
    }

    [Fact]
    public async Task Valid_credentials_log_the_company_in_and_return_a_token()
    {
        var company = CreateCompany(plainPassword: "1234");

        var result = await CreateHandler(company).Handle(
            new LoginCompanyCommand("empresa@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TwoFactorChallenge.ShouldBeNull();

        var session = result.Value.Session.ShouldNotBeNull();
        session.EmployeeId.ShouldBe(company.Id);
        session.CompanyId.ShouldBe(company.Id);
        session.FullName.ShouldBe(company.Name);
        session.Role.ShouldBe("Empresa");
        session.Token.ShouldBe($"token-for-{company.Id}");
        session.ExpiresAt.ShouldBe(FakeTokenService.ExpiresAt);
    }

    [Fact]
    public async Task Wrong_password_returns_the_generic_error()
    {
        var result = await CreateHandler(CreateCompany(plainPassword: "1234")).Handle(
            new LoginCompanyCommand("empresa@empresa.com", "wrong"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Nonexistent_email_returns_the_same_generic_error_as_wrong_password()
    {
        var result = await CreateHandler().Handle(
            new LoginCompanyCommand("nadie@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Deactivated_company_is_rejected_even_with_correct_credentials()
    {
        var result = await CreateHandler(CreateCompany(plainPassword: "1234", isActive: false)).Handle(
            new LoginCompanyCommand("empresa@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Company_with_two_factor_gets_a_challenge_instead_of_a_session()
    {
        var company = CreateCompany(plainPassword: "1234");
        company.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.EnableTwoFactor(Now);

        var result = await CreateHandler(company).Handle(
            new LoginCompanyCommand("empresa@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.ShouldBeNull();
        result.Value.TwoFactorChallenge.ShouldNotBeNull().Value.ShouldBe($"challenge-for-{company.Id}");
    }

    [Fact]
    public async Task Wrong_password_never_reveals_that_two_factor_is_enabled()
    {
        var company = CreateCompany(plainPassword: "1234");
        company.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.EnableTwoFactor(Now);

        var result = await CreateHandler(company).Handle(
            new LoginCompanyCommand("empresa@empresa.com", "wrong"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Enrollment_started_but_not_confirmed_does_not_ask_for_the_code()
    {
        var company = CreateCompany(plainPassword: "1234");
        company.StartTwoFactorEnrollment("SECRETBASE32", Now);

        var result = await CreateHandler(company).Handle(
            new LoginCompanyCommand("empresa@empresa.com", "1234"),
            CancellationToken.None);

        result.Value.Session.ShouldNotBeNull();
    }
}
