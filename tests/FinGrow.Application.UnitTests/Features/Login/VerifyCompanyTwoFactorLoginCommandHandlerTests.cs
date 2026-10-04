namespace FinGrow.Application.UnitTests.Features.Login;

using FinGrow.Application.Features.Login;
using FinGrow.Application.Features.Session;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class VerifyCompanyTwoFactorLoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeTokenService _tokenService = new();

    private Company AddCompanyWithTwoFactor()
    {
        var company = Company.Create(
            "Empresa Demo S.A.",
            TaxId.From("20123456786"),
            Email.From("empresa@empresa.com"),
            "1234",
            Currency.ARS,
            Now);

        company.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.EnableTwoFactor(Now);
        _companies.Companies.Add(company);
        return company;
    }

    private VerifyCompanyTwoFactorLoginCommandHandler CreateHandler() =>
        new(
            _tokenService,
            new FakeTotpService(),
            _companies,
            new SessionIssuer(_tokenService, new FakeRefreshTokenRepository(), new FakeDateTimeProvider(Now)),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_valid_code_completes_the_login_and_issues_the_session()
    {
        var company = AddCompanyWithTwoFactor();
        var challenge = _tokenService.GenerateTwoFactorChallenge(company.Id).Value;

        var result = await CreateHandler().Handle(
            new VerifyCompanyTwoFactorLoginCommand(challenge, FakeTotpService.ValidCode),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.ShouldBe("Empresa");
        result.Value.CompanyId.ShouldBe(company.Id);
    }

    [Fact]
    public async Task An_invalid_code_is_rejected_without_issuing_a_session()
    {
        var company = AddCompanyWithTwoFactor();
        var challenge = _tokenService.GenerateTwoFactorChallenge(company.Id).Value;

        var result = await CreateHandler().Handle(
            new VerifyCompanyTwoFactorLoginCommand(challenge, "000000"),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Auth.CodigoInvalido");
    }

    [Fact]
    public async Task A_challenge_issued_for_someone_else_is_rejected()
    {
        AddCompanyWithTwoFactor();
        var employeeChallenge = _tokenService.GenerateTwoFactorChallenge(Guid.NewGuid()).Value;

        var result = await CreateHandler().Handle(
            new VerifyCompanyTwoFactorLoginCommand(employeeChallenge, FakeTotpService.ValidCode),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }

    [Fact]
    public async Task A_malformed_challenge_is_rejected()
    {
        var result = await CreateHandler().Handle(
            new VerifyCompanyTwoFactorLoginCommand("not-a-challenge", FakeTotpService.ValidCode),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }
}
