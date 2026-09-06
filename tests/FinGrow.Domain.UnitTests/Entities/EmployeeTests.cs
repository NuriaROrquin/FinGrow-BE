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
    public void Un_empleado_nace_activo_y_puede_no_tener_departamento_todavia()
    {
        var employee = CreateEmployee();

        employee.IsActive.ShouldBeTrue();
        employee.DepartmentId.ShouldBeNull();
    }

    [Fact]
    public void Un_empleado_siempre_pertenece_a_una_empresa()
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
    public void Reasignar_de_departamento_solo_cambia_el_id()
    {
        var employee = CreateEmployee();
        var departmentId = Guid.CreateVersion7();

        employee.AssignToDepartment(departmentId, Now.AddDays(1));

        employee.DepartmentId.ShouldBe(departmentId);
        employee.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void Un_empleado_dado_de_baja_no_puede_iniciar_sesion()
    {
        var employee = CreateEmployee();
        employee.Deactivate(Now);

        Should.Throw<DomainException>(() => employee.RegisterLogin(Now.AddHours(1)));
    }

    [Fact]
    public void El_login_de_un_empleado_activo_queda_registrado()
    {
        var employee = CreateEmployee();

        employee.RegisterLogin(Now.AddHours(1));

        employee.LastLoginAt.ShouldBe(Now.AddHours(1));
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
