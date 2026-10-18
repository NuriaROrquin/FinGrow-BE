namespace FinGrow.Application;

using System.Reflection;
using Common.Behaviors;
using Features.Achievements;
using Features.Achievements.Evaluate;
using Events;
using Features.Integrations.ChatTransactions;
using Features.Integrations.Gmail.Sync;
using Features.Integrations.Iol;
using Features.Integrations.Iol.KeepAlive;
using Features.Integrations.Iol.Sync;
using Features.Integrations.Linking;
using Features.Integrations.MercadoPago.Sync;
using Features.Integrations.WhatsApp;
using Features.Investments.AccrueInvestments;
using Features.Investments.QuoteInvestments;
using Features.Metrics.FinancialHealth;
using Features.Metrics;
using Features.Metrics.SnapshotMetrics;
using Features.Notifications;
using Features.Notifications.Alerts.GoalProgress;
using Features.Notifications.Alerts.Company;
using Features.Notifications.Delivery;
using Features.OutboundEmails;
using Features.OutboundEmails.DeliverDueEmails;
using Features.Receipts.Cleanup;
using Features.Receipts.Scanning;
using Features.Receipts.Storing;
using Features.Session;
using FluentValidation;
using Jobs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Features.Account.TwoFactor;
using Features.Departments;

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
        services.AddScoped<ChatActivityRecorder>();
        services.AddScoped<ChatTransactionProposer>();
        services.AddScoped<ChatTransactionReviewer>();
        services.AddScoped<ChatVoiceNoteTranscriber>();
        services.AddScoped<ReceiptStorer>();
        services.AddScoped<ReceiptScanner>();
        services.AddScoped<WhatsAppProposalReviewer>();
        services.AddScoped<MercadoPagoSynchronizer>();
        services.AddScoped<GmailSynchronizer>();
        services.AddScoped<IolAccessTokenProvider>();
        services.AddScoped<IolSynchronizer>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<TwoFactorAccountFinder>();
        services.AddScoped<DepartmentResponseBuilder>();
        services.AddScoped<DepartmentManagerEligibility>();
        services.AddScoped<FinancialHealthScorer>();
        services.AddSingleton<MetricsAnonymizationPolicy>();
        services.AddScoped<AchievementGranter>();
        services.AddScoped<MonthlyAchievementsEvaluator>();

        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<Notifier>();
        services.AddScoped<CompanyAlertsEvaluator>();
        services.AddScoped<INotificationSender, EmailNotificationSender>();
        services.AddScoped<INotificationSender, TelegramNotificationSender>();
        services.AddScoped<OutboundEmailDispatcher>();

        services.AddSingleton<RunningJobs>();
        services.AddScoped<JobRunner>();
        services.AddScoped<IScheduledJob, MetricsSnapshotJob>();
        services.AddScoped<IScheduledJob, MercadoPagoSyncJob>();
        services.AddScoped<IScheduledJob, IolSyncJob>();
        services.AddScoped<IScheduledJob, IolTokenRefreshJob>();
        services.AddScoped<IScheduledJob, GmailSyncJob>();
        services.AddScoped<IScheduledJob, InvestmentQuotesJob>();
        services.AddScoped<IScheduledJob, InvestmentAccrualsJob>();
        services.AddScoped<IScheduledJob, GoalProgressAlertsJob>();
        services.AddScoped<IScheduledJob, CompanyAlertsJob>();
        services.AddScoped<IScheduledJob, EmailDeliveryJob>();
        services.AddScoped<IScheduledJob, ReceiptsCleanupJob>();
        services.AddScoped<IScheduledJob, AchievementsEvaluationJob>();

        return services;
    }
}
