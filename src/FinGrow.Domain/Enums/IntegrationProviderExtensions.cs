namespace FinGrow.Domain.Enums;

public static class IntegrationProviderExtensions
{
    public static bool LinksWithCode(this IntegrationProvider provider) =>
        provider is IntegrationProvider.Telegram or IntegrationProvider.WhatsApp;
}
