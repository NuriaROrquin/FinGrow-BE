namespace FinGrow.Application;

using System.Reflection;
using Common.Behaviors;
using Features.Integrations.Linking;
using Features.Integrations.MercadoPago.Sync;
using FluentValidation;
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

        return services;
    }
}
