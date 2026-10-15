namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Integrations.Iol;
using Microsoft.AspNetCore.WebUtilities;

public class IolPortfolioClientTests
{
    private const string AccessToken = "iol-bearer-secreto";

    // Forma de GET /api/v2/portafolio/{pais} segun la documentacion de IOL; confirmar contra una cuenta real.
    private const string PortfolioResponse = """
        {
          "pais": "argentina",
          "activos": [
            {
              "cantidad": 10,
              "comprometido": 0,
              "puntosVariacion": 120.5,
              "variacionDiaria": 2.2,
              "ultimoPrecio": 5600,
              "ppc": 5012.5,
              "gananciaPorcentaje": 11.72,
              "gananciaDinero": 5875,
              "valorizado": 56000,
              "titulo": {
                "simbolo": "GGAL",
                "descripcion": "Grupo Financiero Galicia",
                "pais": "argentina",
                "mercado": "bCBA",
                "tipo": "ACCIONES",
                "plazo": "t1",
                "moneda": "peso_Argentino"
              },
              "parking": null
            },
            {
              "cantidad": "2",
              "ppc": "15000.75",
              "valorizado": "31000.5",
              "titulo": {
                "simbolo": "AAPL",
                "descripcion": "Cedear Apple Inc.",
                "mercado": "bCBA",
                "tipo": "CEDEARS",
                "moneda": "peso_Argentino"
              }
            },
            { "cantidad": 1, "ppc": 1, "valorizado": 1, "titulo": null }
          ]
        }
        """;

    private const string OperationsResponse = """
        [
          {
            "numero": 123456,
            "fechaOrden": "2026-10-13T10:58:01.473",
            "tipo": "Compra",
            "estado": "terminada",
            "mercado": "BCBA",
            "simbolo": "GGAL",
            "cantidad": 10,
            "monto": 50125,
            "modalidad": "precio_Limite",
            "precio": 5012.5,
            "fechaOperada": "2026-10-14T11:05:12.123",
            "cantidadOperada": 10,
            "precioOperado": 5012.5,
            "montoOperado": 50125,
            "plazo": "t1"
          },
          { "numero": 123457, "fechaOrden": "2026-10-13T10:59:00", "tipo": "Compra", "simbolo": "AAPL", "fechaOperada": null }
        ]
        """;

    [Theory]
    [InlineData(IolCountry.Argentina, "/api/v2/portafolio/argentina")]
    [InlineData(IolCountry.UnitedStates, "/api/v2/portafolio/estados_Unidos")]
    public async Task The_portfolio_is_requested_by_country_with_the_bearer(IolCountry country, string path)
    {
        using var handler = new RecordingHandler(PortfolioResponse);

        await Client(handler).GetPortfolioAsync(AccessToken, country);

        handler.Path.ShouldBe(path);
        handler.Authorization.ShouldBe($"Bearer {AccessToken}");
    }

    [Fact]
    public async Task Each_asset_becomes_a_position_with_the_fields_of_its_title()
    {
        using var handler = new RecordingHandler(PortfolioResponse);

        var positions = await Client(handler).GetPortfolioAsync(AccessToken, IolCountry.Argentina);

        positions.Count.ShouldBe(2);
        positions[0].ShouldBe(new IolPosition("GGAL", "Grupo Financiero Galicia", "ACCIONES", "bCBA", "peso_Argentino", 10m, 5012.5m, 56000m));
        positions[1].ShouldBe(new IolPosition("AAPL", "Cedear Apple Inc.", "CEDEARS", "bCBA", "peso_Argentino", 2m, 15000.75m, 31000.5m));
    }

    [Fact]
    public async Task An_empty_portfolio_has_no_positions()
    {
        using var handler = new RecordingHandler("""{"pais": "estados_Unidos", "activos": []}""");

        var positions = await Client(handler).GetPortfolioAsync(AccessToken, IolCountry.UnitedStates);

        positions.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_completed_operations_are_filtered_by_state_and_date()
    {
        using var handler = new RecordingHandler(OperationsResponse);

        await Client(handler).ListCompletedOperationsAsync(AccessToken, new DateOnly(2016, 10, 13));

        handler.Path.ShouldBe("/api/v2/operaciones");
        var query = QueryHelpers.ParseQuery(handler.Query);
        query["filtro.estado"].ToString().ShouldBe("terminadas");
        query["filtro.fechaDesde"].ToString().ShouldBe("2016-10-13");
        handler.Authorization.ShouldBe($"Bearer {AccessToken}");
    }

    [Fact]
    public async Task An_operation_takes_the_day_it_was_executed_or_else_the_day_it_was_ordered()
    {
        using var handler = new RecordingHandler(OperationsResponse);

        var operations = await Client(handler).ListCompletedOperationsAsync(AccessToken, new DateOnly(2016, 10, 13));

        operations.ShouldBe(new[]
        {
            new IolOperation("GGAL", "Compra", new DateOnly(2026, 10, 14)),
            new IolOperation("AAPL", "Compra", new DateOnly(2026, 10, 13)),
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task A_failure_keeps_the_status_code_and_never_carries_the_bearer(HttpStatusCode status)
    {
        using var handler = new RecordingHandler("""{"message": "error"}""", status);

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            Client(handler).GetPortfolioAsync(AccessToken, IolCountry.Argentina));

        exception.StatusCode.ShouldBe(status);
        exception.Message.ShouldNotContain(AccessToken);
    }

    private static IolPortfolioClient Client(RecordingHandler handler) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.iol.test/") });

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _status;

        public RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK)
        {
            _response = response;
            _status = status;
        }

        public string? Path { get; private set; }

        public string Query { get; private set; } = string.Empty;

        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            Query = request.RequestUri?.Query ?? string.Empty;
            Authorization = request.Headers.Authorization?.ToString();

            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            });
        }
    }
}
