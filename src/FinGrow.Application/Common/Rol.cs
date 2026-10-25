namespace FinGrow.Application.Common;

using FinGrow.Domain.Enums;

public static class Rol
{
    public const string Empleado = "Empleado";

    public const string Empresa = "Empresa";

    public static string From(UserRole role) => role == UserRole.Company ? Empresa : Empleado;
}
