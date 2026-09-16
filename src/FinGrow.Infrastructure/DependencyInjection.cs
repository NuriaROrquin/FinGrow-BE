namespace FinGrow.Infrastructure;

using System.Net.Http.Headers;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Infrastructure.Ai;
using FinGrow.Infrastructure.Identity;
using FinGrow.Infrastructure.Integrations.Twilio;
using FinGrow.Infrastructure.Persistence;
using FinGrow.Infrastructure.Persistence.Repositories;
using FinGrow.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddAiService(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddTwilio(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion 'ConnectionStrings:Database' en la configuracion.");

        services.AddDbContext<FinGrowDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(FinGrowDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FinGrowDbContext>());
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();

        services.AddScoped<DatabaseSeeder>();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeIntegrationRepository, EmployeeIntegrationRepository>();
        services.AddScoped<IIntegrationLinkCodeRepository, IntegrationLinkCodeRepository>();

        return services;
    }

    private static IServiceCollection AddAiService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiServiceOptions>()
            .Bind(configuration.GetSection(AiServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IAiService, AiService>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AiServiceOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add(AiServiceOptions.ApiKeyHeader, options.ApiKey);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TwilioOptions>()
            .Bind(configuration.GetSection(TwilioOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITwilioRequestValidator, TwilioRequestValidator>();

        services.AddHttpClient<ITwilioMediaClient, TwilioMediaClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<TwilioOptions>>().Value;
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.AccountSid}:{options.AuthToken}"));

                client.Timeout = TimeSpan.FromSeconds(options.MediaTimeoutSeconds);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var options = jwtOptions.Value;

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        return services;
    }
}
