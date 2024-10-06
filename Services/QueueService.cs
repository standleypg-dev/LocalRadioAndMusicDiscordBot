using Discord.WebSocket;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;

namespace radio_discord_bot.Services;

public class QueueService(GlobalStore globalStore, IYoutubeService youtubeService) : IQueueService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));

    public async Task AddSongAsync(Song song)
    {
        try
        {
            // if the store doesn't have a queue, create a new instance of the Queue store
            if (!_globalStore.TryGet<Queue<Song>>(out _))
            {
                _globalStore.Set(new Queue<Song>());
                _globalStore.Get<Queue<Song>>()?.Enqueue(song);
            }
            else
            {
                _globalStore.Get<Queue<Song>>()?.Enqueue(song);
            }

            var songTitle = await youtubeService.GetVideoTitleAsync(song.Url);
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Added {songTitle} to the queue.");
        }
        catch (Exception ex)
        {
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Error on AddSongAsync: {ex.Message}");
        }
    }

    public async Task SkipSongAsync()
    {
        if (_globalStore.Get<Queue<Song>>()?.Count > 0)
            _globalStore.Get<Queue<Song>>()?.Dequeue();
        else
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                "There are no songs in the queue.");
    }

    public async Task ClearQueueAsync()
    {
        await Task.CompletedTask;
        _globalStore.Get<Queue<Song>>()?.Clear();
    }

    public async Task<List<string>> GetQueueAsync()
    {
        var songTitles = new List<string>();
        foreach (var songs in globalStore.Get<Queue<Song>>()?.Select(x => x.Url).ToList() ?? [])
        {
            songTitles.Add(await youtubeService.GetVideoTitleAsync(songs));
        }

        return songTitles;
    }
}