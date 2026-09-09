namespace FinGrow.Api.Extensions;

using FinGrow.Application.Common;
using Microsoft.AspNetCore.Mvc;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : result.Error.ToProblemResult();

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : result.Error.ToProblemResult();

    private static ObjectResult ToProblemResult(this Error error)
    {
        var statusCode = MapStatusCode(error.Type);

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Description,
        })
        {
            StatusCode = statusCode,
        };
    }

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };
}
