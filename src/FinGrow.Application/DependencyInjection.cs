namespace FinGrow.Application;

using System.Reflection;
using Common.Behaviors;
using Features.Integrations.Linking;
using Features.Integrations.MercadoPago.Sync;
using Features.Metrics.SnapshotMetrics;
using Features.Session;
using FluentValidation;
using Jobs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<LinkCodeIssuer>();
        services.AddScoped<LinkCodeRedeemer>();
        services.AddScoped<MercadoPagoSynchronizer>();
        services.AddScoped<SessionIssuer>();

        services.AddSingleton<RunningJobs>();
        services.AddScoped<JobRunner>();
        services.AddScoped<IScheduledJob, MetricsSnapshotJob>();
        services.AddScoped<IScheduledJob, MercadoPagoSyncJob>();

        return services;
    }
}
