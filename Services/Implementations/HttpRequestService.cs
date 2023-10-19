using System.Text.Json;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services.Implementations;

public class HttpRequestService : IHttpRequestService
{
    public async Task<T> GetAsync<T>(string url)
    {
        using (HttpClient _httpClient = new HttpClient())
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content) ?? throw new HttpRequestException($"Error deserializing data from the server. Status code: {response.StatusCode}");
        }

    }
}
