namespace FinGrow.Infrastructure.Identity;

using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

internal sealed class HttpClientDevice : IClientDevice
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientDevice(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string? UserAgent
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.UserAgent].ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
