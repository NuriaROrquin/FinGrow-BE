namespace FinGrow.Application;

using System.Reflection;
using Common.Behaviors;
using Events;
using Features.Integrations.ChatTransactions;
using Features.Integrations.Linking;
using Features.Integrations.MercadoPago.Sync;
using Features.Investments.QuoteInvestments;
using Features.Metrics.SnapshotMetrics;
using Features.Notifications;
using Features.Notifications.Alerts.GoalProgress;
using Features.Notifications.Delivery;
using Features.Session;
using FluentValidation;
using Jobs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Features.Account.TwoFactor;

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
        services.AddScoped<ChatTransactionProposer>();
        services.AddScoped<ChatTransactionReviewer>();
        services.AddScoped<MercadoPagoSynchronizer>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<TwoFactorAccountFinder>();

        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<Notifier>();
        services.AddScoped<INotificationSender, TelegramNotificationSender>();

        services.AddSingleton<RunningJobs>();
        services.AddScoped<JobRunner>();
        services.AddScoped<IScheduledJob, MetricsSnapshotJob>();
        services.AddScoped<IScheduledJob, MercadoPagoSyncJob>();
        services.AddScoped<IScheduledJob, InvestmentQuotesJob>();
        services.AddScoped<IScheduledJob, GoalProgressAlertsJob>();

        return services;
    }
}
