namespace FinGrow.Application.UnitTests.Features.CompanyProfile;

using FinGrow.Application.Features.CompanyProfile.UpdateCompanyProfile;

public sealed class UpdateCompanyProfileCommandValidatorTests
{
    private readonly UpdateCompanyProfileCommandValidator _validator = new();

    [Fact]
    public void Validator_rejects_an_invalid_tax_id()
    {
        var result = _validator.Validate(new UpdateCompanyProfileCommand(
            "Empresa Demo S.A.",
            "20123456787",
            "contacto@empresa.com",
            null,
            null,
            null,
            null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateCompanyProfileCommand.TaxId));
    }

    [Fact]
    public void Validator_accepts_a_valid_tax_id_and_corporate_information()
    {
        var result = _validator.Validate(new UpdateCompanyProfileCommand(
            "Empresa Demo S.A.",
            "20123456786",
            "contacto@empresa.com",
            "Servicios profesionales",
            "+54 11 1234-5678",
            "Av. Siempre Viva 742",
            2005));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validator_rejects_an_industry_outside_the_catalog()
    {
        var result = _validator.Validate(new UpdateCompanyProfileCommand(
            "Empresa Demo S.A.",
            "20123456786",
            "contacto@empresa.com",
            "Industria inexistente",
            null,
            null,
            null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateCompanyProfileCommand.Industry));
    }
}
