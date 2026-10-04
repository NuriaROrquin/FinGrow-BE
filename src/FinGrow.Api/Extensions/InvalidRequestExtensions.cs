namespace FinGrow.Api.Extensions;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Un cuerpo que no se puede deserializar (un <c>"abc"</c> donde va un importe) nunca llega al
/// validador: lo corta el model binding con un mensaje en ingles pensado para el desarrollador.
/// Esto lo traduce al mismo formato que <see cref="ResultExtensions"/>, para que el frontend
/// muestre <c>detail</c> sin distinguir de donde vino el error.
/// </summary>
public static class InvalidRequestExtensions
{
    private const string GeneralMessage = "La solicitud no tiene el formato esperado.";

    private static readonly Type[] NumericTypes =
    {
        typeof(decimal),
        typeof(double),
        typeof(float),
        typeof(int),
        typeof(long),
        typeof(short),
    };

    public static IMvcBuilder AddReadableInvalidRequestResponses(this IMvcBuilder builder) =>
        builder.ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = CreateResponse);

    private static ObjectResult CreateResponse(ActionContext context)
    {
        var bodyType = context.ActionDescriptor.Parameters
            .FirstOrDefault(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            ?.ParameterType;

        // System.Text.Json registra cada error con la ruta JSON del campo como clave ("$.amount").
        var fieldErrors = context.ModelState
            .Where(entry => entry.Key.StartsWith("$.", StringComparison.Ordinal) && entry.Value is { Errors.Count: > 0 })
            .Select(entry => Describe(entry.Key[2..], bodyType))
            .ToList();

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation.InvalidFormat",
            Detail = fieldErrors.Count > 0 ? string.Join(" ", fieldErrors) : GeneralMessage,
        };

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
    }

    // El tipo se busca en el contrato y no en el mensaje de System.Text.Json: cuando el cuerpo
    // es un record con constructor, el mensaje nombra al record y no al tipo del campo.
    private static string Describe(string field, Type? bodyType) =>
        ResolveFieldType(bodyType, field) is { } type && NumericTypes.Contains(type)
            ? $"El campo '{field}' tiene que ser un numero."
            : $"El campo '{field}' tiene un valor que no es valido.";

    private static Type? ResolveFieldType(Type? type, string path)
    {
        foreach (var segment in path.Split('.'))
        {
            if (type is null)
            {
                return null;
            }

            var bracket = segment.IndexOf('[', StringComparison.Ordinal);
            var name = bracket < 0 ? segment : segment[..bracket];

            type = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.PropertyType;

            if (bracket >= 0 && type is not null)
            {
                type = ElementType(type);
            }
        }

        return type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
    }

    private static Type? ElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        return collectionType.IsGenericType ? collectionType.GetGenericArguments()[0] : null;
    }
}
