namespace FinGrow.Api.UnitTests.Extensions;

using FinGrow.Api.Extensions;
using FinGrow.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class ResultExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void A_failed_Result_maps_to_the_correct_status_code(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Code", "Description", errorType);
        var result = Result.Failure(error);

        var actionResult = result.ToActionResult();

        var objectResult = actionResult.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(expectedStatusCode);
        var problemDetails = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Detail.ShouldBe(error.Description);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void A_failed_generic_Result_maps_to_the_correct_status_code(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Code", "Description", errorType);
        var result = Result.Failure<string>(error);

        var actionResult = result.ToActionResult();

        var objectResult = actionResult.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public void A_successful_Result_returns_NoContent()
    {
        var result = Result.Success();

        var actionResult = result.ToActionResult();

        actionResult.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public void A_successful_generic_Result_returns_Ok_with_the_value()
    {
        var result = Result.Success("value");

        var actionResult = result.ToActionResult();

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe("value");
    }
}
