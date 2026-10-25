namespace FinGrow.Application.UnitTests.Features.CompanyProfile;

using FinGrow.Application.Common;
using FinGrow.Application.Features.CompanyProfile.UpdateCompanyProfile;

public class UpdateCompanyProfileCommandHandlerTests
{
    private readonly CompanyStaffScenario _scenario = new();

    private UpdateCompanyProfileCommandHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _scenario.Employees,
            _scenario.Users,
            _scenario.UnitOfWork,
            _scenario.Clock);

    private static UpdateCompanyProfileCommand CommandWithEmail(string email) =>
        new("Acme S.A.", "20123456786", email, null, null, null, null);

    [Fact]
    public async Task The_company_can_keep_its_own_email()
    {
        var result = await CreateHandler().Handle(CommandWithEmail("rrhh@acme.com"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("rrhh@acme.com");
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_new_email_changes_the_login_of_the_company()
    {
        var result = await CreateHandler().Handle(CommandWithEmail("personas@acme.com"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _scenario.Company.User.Email.Value.ShouldBe("personas@acme.com");
    }

    [Fact]
    public async Task An_email_used_by_an_employee_is_rejected_with_conflict()
    {
        _scenario.AddEmployee(department: null, email: "ana@acme.com");

        var result = await CreateHandler().Handle(CommandWithEmail("ana@acme.com"), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("CompanyProfile.EmailTaken");
        _scenario.Company.User.Email.Value.ShouldBe("rrhh@acme.com");
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
    }
}
