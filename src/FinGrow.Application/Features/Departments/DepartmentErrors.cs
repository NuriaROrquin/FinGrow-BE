namespace FinGrow.Application.Features.Departments;

using FinGrow.Application.Common;

internal static class DepartmentErrors
{
    public static readonly Error NoAutenticado =
        Error.Unauthorized("Department.NoAutenticado", "Hay que iniciar sesión como empresa para administrar los departamentos.");

    public static Error NotFound(Guid departmentId) =>
        Error.NotFound("Department.NotFound", $"No existe un departamento con id '{departmentId}' en tu empresa.");

    public static Error NameTaken(string name) =>
        Error.Conflict("Department.NameTaken", $"Ya existe un departamento llamado '{name.Trim()}'.");

    public static readonly Error ManagerNotAvailable =
        Error.Validation(
            "Department.ManagerNotAvailable",
            "El responsable tiene que ser un empleado activo de tu empresa.");

    public static readonly Error InvalidMoveTarget =
        Error.Validation(
            "Department.InvalidMoveTarget",
            "Para mover a sus empleados elegí otro departamento activo de tu empresa.");

    public static Error HasEmployees(int employees) =>
        Error.Conflict(
            "Department.HasEmployees",
            employees == 1
                ? "No se puede eliminar: tiene 1 empleado asignado. Reasignalo a otro departamento antes de eliminarlo."
                : $"No se puede eliminar: tiene {employees} empleados asignados. Reasignalos a otro departamento antes de eliminarlo.");
}
