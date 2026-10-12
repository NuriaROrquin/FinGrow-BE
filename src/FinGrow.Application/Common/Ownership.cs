namespace FinGrow.Application.Common;

internal static class Ownership
{
    /// <summary>
    /// Resuelve el acceso de un empleado a un recurso suyo. Un recurso inexistente (o de otra empresa,
    /// que el filtro global ya no deja ver) es <c>NotFound</c>; uno que existe pero es de otro empleado
    /// es <c>Forbidden</c>.
    /// </summary>
    public static Result<TResource> Require<TResource>(
        TResource? resource,
        Func<TResource, Guid> ownerOf,
        Guid employeeId,
        Error notFound,
        Error notOwned)
        where TResource : class
    {
        if (resource is null)
        {
            return Result.Failure<TResource>(notFound);
        }

        return ownerOf(resource) == employeeId
            ? Result.Success(resource)
            : Result.Failure<TResource>(notOwned);
    }
}
