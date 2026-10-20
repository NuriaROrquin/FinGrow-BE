namespace FinGrow.Application.Features.PasswordReset;

using FinGrow.Application.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Empleados y empresas guardan su contrasena en tablas distintas hasta T-37 (tabla <c>users</c>):
/// esto los junta para que la recuperacion sea un solo flujo. Solo devuelve cuentas que podrian
/// iniciar sesion: una desactivada (o un empleado dado de baja, que tambien queda inactivo) no
/// recibe enlace ni puede canjear uno pendiente.
/// </summary>
internal sealed record ResettableAccount(
    Guid Id,
    Guid CompanyId,
    string Role,
    string Name,
    Email Email,
    Action<string, DateTimeOffset> ChangePassword)
{
    public static async Task<ResettableAccount?> FindByEmailAsync(
        string role,
        Email email,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        CancellationToken cancellationToken) =>
        role == Rol.Empresa
            ? From(await companies.GetByEmailAsync(email, cancellationToken))
            : From(await employees.GetByEmailAsync(email, cancellationToken));

    public static async Task<ResettableAccount?> FindByIdAsync(
        string role,
        Guid id,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        CancellationToken cancellationToken) =>
        role == Rol.Empresa
            ? From(await companies.GetByIdAsync(id, cancellationToken))
            : From(await employees.GetByIdAsync(id, cancellationToken));

    private static ResettableAccount? From(Employee? employee) =>
        employee is { IsActive: true }
            ? new ResettableAccount(employee.Id, employee.CompanyId, Rol.Empleado, employee.FullName, employee.Email, employee.ChangePassword)
            : null;

    private static ResettableAccount? From(Company? company) =>
        company is { IsActive: true }
            ? new ResettableAccount(company.Id, company.Id, Rol.Empresa, company.Name, company.Email, company.ChangePassword)
            : null;
}
