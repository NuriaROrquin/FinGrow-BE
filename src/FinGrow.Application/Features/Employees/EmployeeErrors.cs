namespace FinGrow.Application.Features.Employees;

using FinGrow.Application.Common;
using FinGrow.Domain.ValueObjects;

internal static class EmployeeErrors
{
    public static readonly Error NoAutenticado =
        Error.Unauthorized("Employee.NoAutenticado", "Hay que iniciar sesión como empresa para administrar los empleados.");

    public static readonly Error DepartmentNotAvailable =
        Error.Validation("Employee.DepartmentNotAvailable", "El departamento elegido no existe en tu empresa o está inactivo.");

    public static Error NotFound(Guid employeeId) =>
        Error.NotFound("Employee.NotFound", $"No existe un empleado con id '{employeeId}' en tu empresa.");

    public static Error EmailTaken(Email email) =>
        Error.Conflict("Employee.EmailTaken", $"Ya hay una cuenta con el email '{email}'.");
}
