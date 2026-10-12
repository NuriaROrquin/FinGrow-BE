using System.Text.Json.Serialization;
using FinGrow.Api.Authentication;
using FinGrow.Api.Extensions;
using FinGrow.Api.Gmail;
using FinGrow.Api.Jobs;
using FinGrow.Api.MercadoPago;
using FinGrow.Api.Notifications;
using FinGrow.Application;
using FinGrow.Application.Features.Metrics;
using FinGrow.Infrastructure;
using FinGrow.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<MetricsAnonymizationOptions>()
    .Bind(builder.Configuration.GetSection(MetricsAnonymizationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSessionCookieAuthentication();
builder.Services.AddNotificationsHub();
builder.Services.AddOptions<MercadoPagoReturnOptions>()
    .Bind(builder.Configuration.GetSection(MercadoPagoReturnOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<GmailReturnOptions>()
    .Bind(builder.Configuration.GetSection(GmailReturnOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<JobsOptions>()
    .Bind(builder.Configuration.GetSection(JobsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddReadableInvalidRequestResponses();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<FinGrowDbContext>(name: "database");

const string frontendCorsPolicy = "FinGrowFrontend";
builder.Services.AddCors(options =>
    options.AddPolicy(frontendCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

await app.MigrateDatabaseAsync();
await app.SeedDatabaseAsync();

app.UseExceptionHandling();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapNotificationsHub();
app.MapHealthChecks("/health");

await app.RunAsync();

public partial class Program;
