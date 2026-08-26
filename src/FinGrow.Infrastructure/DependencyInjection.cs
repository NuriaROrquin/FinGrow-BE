namespace FinGrow.Infrastructure;

using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Ai;
using FinGrow.Infrastructure.Identity;
using FinGrow.Infrastructure.Persistence;
using FinGrow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddAiService(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

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
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
