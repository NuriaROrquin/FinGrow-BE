namespace FinGrow.Application.UnitTests.Features.Session;

using FinGrow.Application.Features.Session.ListSessions;

public class UserAgentDescriptionTests
{
    [Theory]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
        "Chrome",
        "Windows")]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0",
        "Edge",
        "Windows")]
    [InlineData(
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15",
        "Safari",
        "macOS")]
    [InlineData(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/141.0 Mobile/15E148 Safari/604.1",
        "Chrome",
        "iOS")]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 15; SM-S921B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/28.0 Chrome/130.0 Mobile Safari/537.36",
        "Samsung Internet",
        "Android")]
    [InlineData("Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0", "Firefox", "Linux")]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 OPR/122.0",
        "Opera",
        "Windows")]
    public void Recognizes_the_usual_browsers_and_systems(string userAgent, string browser, string operatingSystem)
    {
        UserAgentDescription.BrowserOf(userAgent).ShouldBe(browser);
        UserAgentDescription.OperatingSystemOf(userAgent).ShouldBe(operatingSystem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("curl/8.9.1")]
    public void Does_not_guess_what_it_does_not_recognize(string? userAgent)
    {
        UserAgentDescription.BrowserOf(userAgent).ShouldBeNull();
        UserAgentDescription.OperatingSystemOf(userAgent).ShouldBeNull();
    }
}
