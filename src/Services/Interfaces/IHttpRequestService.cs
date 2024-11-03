using radio_discord_bot.Enums;

namespace radio_discord_bot.Services.Interfaces;

public interface IHttpRequestService
{
    Task<T> GetAsync<T>(string url, object? data = null, string? token = null);
    Task<T> PostAsync<T>(string url, object data, PostRequestMediaType mediaType = PostRequestMediaType.Json);
}
