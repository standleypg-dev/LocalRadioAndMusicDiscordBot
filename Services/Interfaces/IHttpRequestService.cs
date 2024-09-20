namespace radio_discord_bot.Services.Interfaces;

public interface IHttpRequestService
{
    Task<T> GetAsync<T>(string url);
}
