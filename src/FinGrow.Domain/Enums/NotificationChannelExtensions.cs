namespace FinGrow.Domain.Enums;

public static class NotificationChannelExtensions
{
    public static bool IsConfigurable(this NotificationChannel channel) => channel != NotificationChannel.InApp;
}
