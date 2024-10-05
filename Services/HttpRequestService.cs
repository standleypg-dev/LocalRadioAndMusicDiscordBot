using System.Text.Json;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services;

public class HttpRequestService : IHttpRequestService
{
    public async Task<T> GetAsync<T>(string url)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content) ?? throw new HttpRequestException($"Error deserializing data from the server. Status code: {response.StatusCode}");
    }
}
