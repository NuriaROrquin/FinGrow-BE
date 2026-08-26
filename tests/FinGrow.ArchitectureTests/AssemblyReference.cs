namespace FinGrow.ArchitectureTests;

using System.Reflection;

internal static class AssemblyReference
{
    public static readonly Assembly Domain = typeof(FinGrow.Domain.Common.Entity).Assembly;

    public static readonly Assembly Application = typeof(FinGrow.Application.DependencyInjection).Assembly;

    public static readonly Assembly Infrastructure = typeof(FinGrow.Infrastructure.DependencyInjection).Assembly;

    public static readonly Assembly Api = typeof(Program).Assembly;
}
