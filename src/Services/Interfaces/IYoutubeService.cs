namespace radio_discord_bot.Services.Interfaces;

public interface IYoutubeService
{
    Task<string> GetAudioStreamUrlAsync(string url);
    Task<string> GetVideoTitleAsync(string url);
}