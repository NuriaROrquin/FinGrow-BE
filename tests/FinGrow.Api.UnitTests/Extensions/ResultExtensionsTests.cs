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
    public void Un_Result_fallido_mapea_al_status_code_correcto(ErrorType errorType, int statusCodeEsperado)
    {
        var error = new Error("Codigo", "Descripcion", errorType);
        var result = Result.Failure(error);

        var actionResult = result.ToActionResult();

        var objectResult = actionResult.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(statusCodeEsperado);
        var problemDetails = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Detail.ShouldBe(error.Description);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void Un_Result_generico_fallido_mapea_al_status_code_correcto(ErrorType errorType, int statusCodeEsperado)
    {
        var error = new Error("Codigo", "Descripcion", errorType);
        var result = Result.Failure<string>(error);

        var actionResult = result.ToActionResult();

        var objectResult = actionResult.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(statusCodeEsperado);
    }

    [Fact]
    public void Un_Result_exitoso_devuelve_NoContent()
    {
        var result = Result.Success();

        var actionResult = result.ToActionResult();

        actionResult.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public void Un_Result_generico_exitoso_devuelve_Ok_con_el_valor()
    {
        var result = Result.Success("valor");

        var actionResult = result.ToActionResult();

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe("valor");
    }
}
