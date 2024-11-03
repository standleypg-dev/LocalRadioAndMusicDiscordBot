using radio_discord_bot.Services.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace radio_discord_bot.Services;

public class YoutubeService(YoutubeClient youtubeClient) : IYoutubeService
{
    public async Task<string> GetAudioStreamUrlAsync(string url)
    {
        return (await youtubeClient.Videos.Streams.GetManifestAsync(url)).GetAudioOnlyStreams().GetWithHighestBitrate().Url;
    }

    public async Task<string> GetVideoTitleAsync(string url)
    {
        return (await youtubeClient.Videos.GetAsync(url)).Title;
    }
}