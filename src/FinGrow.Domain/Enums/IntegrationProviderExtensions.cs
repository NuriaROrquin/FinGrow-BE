namespace FinGrow.Domain.Enums;

public static class IntegrationProviderExtensions
{
    public static bool LinksWithCode(this IntegrationProvider provider) =>
        provider is IntegrationProvider.Telegram or IntegrationProvider.WhatsApp;

    public static bool LinksWithOAuth(this IntegrationProvider provider) =>
        provider is IntegrationProvider.Gmail or IntegrationProvider.MercadoPago;
}
