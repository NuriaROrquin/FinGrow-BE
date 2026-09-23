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

    private static SessionIssuer CreateSessionIssuer() =>
        new(new FakeTokenService(), new FakeRefreshTokenRepository(), new FakeDateTimeProvider(Now));

    [Fact]
    public async Task Valid_credentials_log_the_company_in_and_return_a_token()
    {
        var company = CreateCompany(plainPassword: "1234");
        var companyRepository = new FakeCompanyRepository();
        companyRepository.Companies.Add(company);

        var handler = new LoginCompanyCommandHandler(
            companyRepository,
            new FakePasswordHasher(),
            CreateSessionIssuer(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCompanyCommand("empresa@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmployeeId.ShouldBe(company.Id);
        result.Value.CompanyId.ShouldBe(company.Id);
        result.Value.FullName.ShouldBe(company.Name);
        result.Value.Role.ShouldBe("Empresa");
        result.Value.Token.ShouldBe($"token-for-{company.Id}");
        result.Value.ExpiresAt.ShouldBe(FakeTokenService.ExpiresAt);
    }

    [Fact]
    public async Task Wrong_password_returns_the_generic_error()
    {
        var company = CreateCompany(plainPassword: "1234");
        var companyRepository = new FakeCompanyRepository();
        companyRepository.Companies.Add(company);

        var handler = new LoginCompanyCommandHandler(
            companyRepository,
            new FakePasswordHasher(),
            CreateSessionIssuer(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCompanyCommand("empresa@empresa.com", "wrong"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Nonexistent_email_returns_the_same_generic_error_as_wrong_password()
    {
        var handler = new LoginCompanyCommandHandler(
            new FakeCompanyRepository(),
            new FakePasswordHasher(),
            CreateSessionIssuer(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCompanyCommand("nadie@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Deactivated_company_is_rejected_even_with_correct_credentials()
    {
        var company = CreateCompany(plainPassword: "1234", isActive: false);
        var companyRepository = new FakeCompanyRepository();
        companyRepository.Companies.Add(company);

        var handler = new LoginCompanyCommandHandler(
            companyRepository,
            new FakePasswordHasher(),
            CreateSessionIssuer(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCompanyCommand("empresa@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }
}
