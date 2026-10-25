namespace FinGrow.Api.Logging;

using Serilog;
using Serilog.Sinks.OpenTelemetry;

public static class AxiomLoggingExtensions
{
    public static LoggerConfiguration WriteToAxiom(
        this LoggerConfiguration logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection(AxiomOptions.SectionName).Get<AxiomOptions>() ?? new AxiomOptions();

        if (!options.IsEnabled)
        {
            return logger;
        }

        options.EnsureComplete();

        return logger.WriteTo.OpenTelemetry(sink =>
        {
            sink.Endpoint = options.LogsEndpoint;
            sink.Protocol = OtlpProtocol.HttpProtobuf;
            sink.Headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {options.ApiToken}",
                [AxiomOptions.DatasetHeader] = options.Dataset,
            };
            sink.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = AxiomOptions.ServiceName,
                ["deployment.environment"] = environment.EnvironmentName,
            };
        });
    }
}
