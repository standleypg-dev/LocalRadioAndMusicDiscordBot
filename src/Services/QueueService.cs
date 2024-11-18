using Discord.WebSocket;
using radio_discord_bot.Models;
using radio_discord_bot.Models.Spotify;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace radio_discord_bot.Services;

public delegate void OnSongAdded(string title);

public class QueueService(
    GlobalStore globalStore,
    IYoutubeService youtubeService,
    YoutubeClient youtubeClient) : IQueueService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    public event OnSongAdded? SongAdded;

    public async Task AddSongAsync(Song song)
    {
        try
        {
            if (!Uri.TryCreate(song.Url, UriKind.Absolute, out _))
            {
                var item = _globalStore.Get<Items[]>()?.ToList().Find(x => x.Id == song.Url);
                var videos = await youtubeClient.Search.GetVideosAsync(item.Name).CollectAsync(1);
                song.Url = videos[0].Url;
            }

            var songTitle = await youtubeService.GetVideoTitleAsync(song.Url);
            song.Title = songTitle;

            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Added {songTitle} to the queue.");
            SongAdded?.Invoke(songTitle);

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AddSongAsync: {ex.Message}");
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

    public async Task<List<Song>> GetQueueAsync()
    {
        await Task.CompletedTask;
        return globalStore.Get<Queue<Song>>()?.ToList() ?? [];
    }
}