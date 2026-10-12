namespace FinGrow.Api.Notifications;

using System.Text.Json.Serialization;
using FinGrow.Application.Features.Notifications.Delivery;
using Microsoft.AspNetCore.SignalR;

public static class NotificationsHubExtensions
{
    public static IServiceCollection AddNotificationsHub(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<IUserIdProvider, NotificationRecipientUserIdProvider>();
        services.AddScoped<INotificationSender, SignalRNotificationSender>();

        return services;
    }

    public static HubEndpointConventionBuilder MapNotificationsHub(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHub<NotificationsHub>(
            NotificationsHub.Path,
            options => options.CloseOnAuthenticationExpiration = true);
}
