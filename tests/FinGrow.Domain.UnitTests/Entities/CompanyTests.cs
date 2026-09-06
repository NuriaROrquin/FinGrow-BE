namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class CompanyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Una_empresa_nace_activa()
    {
        CreateCompany().IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Un_departamento_se_agrega_al_agregado_de_la_empresa()
    {
        var company = CreateCompany();

        var department = company.AddDepartment("Ventas", "Ventas y atencion al cliente", "Maria Gonzalez", Now);

        company.Departments.ShouldContain(department);
        department.CompanyId.ShouldBe(company.Id);
        department.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void No_se_pueden_tener_dos_departamentos_con_el_mismo_nombre()
    {
        var company = CreateCompany();
        company.AddDepartment("Ventas", null, null, Now);

        Should.Throw<DomainException>(() => company.AddDepartment("  ventas ", null, null, Now));
    }

    [Fact]
    public void Un_departamento_se_desactiva_en_lugar_de_borrarse()
    {
        var company = CreateCompany();
        var department = company.AddDepartment("Logistica", null, null, Now);

        department.Deactivate(Now.AddDays(1));

        department.IsActive.ShouldBeFalse();
        company.Departments.ShouldContain(department);
    }

    [Fact]
    public void Una_empresa_sin_razon_social_no_se_crea()
    {
        Should.Throw<DomainException>(() => Company.Create(
            "   ",
            TaxId.From("20123456786"),
            Email.From("hola@empresa.com"),
            "hash",
            Currency.ARS,
            Now));
    }

    private static Company CreateCompany() => Company.Create(
        "Empresa Demo S.A.",
        TaxId.From("20123456786"),
        Email.From("hola@empresa.com"),
        "hash-bcrypt",
        Currency.ARS,
        Now);
}
