using Application.DTOs;
using Application.Interfaces.Services;
using Application.Store;
using ApplicationDto.DTOs;
using ApplicationDto.DTOs.Spotify;
using Discord.WebSocket;
using Infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Infrastructure.Services;


public class QueueService(
    GlobalStore globalStore,
    IServiceProvider serviceProvider) : IQueueService<SongDto<SocketVoiceChannel>>
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    public event OnSongAdded? SongAdded;

    public async Task AddSongAsync(SongDto<SocketVoiceChannel> songDto)
    {
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        var youtubeService = scope.ServiceProvider.GetRequiredService<IYoutubeService>();
        try
        {
            if (!Uri.TryCreate(songDto.Url, UriKind.Absolute, out _))
            {
                var item = _globalStore.Get<Items[]>()?.ToList().Find(x => x.Id == songDto.Url);
                var videos = await youtubeClient.Search.GetVideosAsync(item.Name).CollectAsync(1);
                songDto.Url = videos[0].Url;
            }

            var songTitle = await youtubeService.GetVideoTitleAsync(songDto.Url);
            songDto.Title = songTitle;

            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Added {songTitle} to the queue.");
            SongAdded?.Invoke(songTitle);

            // if the store doesn't have a queue, create a new instance of the Queue store
            if (!_globalStore.TryGet<Queue<SongDto<SocketVoiceChannel>>>(out _))
            {
                _globalStore.Set(new Queue<SongDto<SocketVoiceChannel>>());
                _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>()?.Enqueue(songDto);
            }
            else
            {
                _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>()?.Enqueue(songDto);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AddSongAsync: {ex.Message}");
        }
    }

    public async Task SkipSongAsync()
    {
        if (_globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>()?.Count > 0)
            _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>()?.Dequeue();
        else
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                "There are no songs in the queue.");
    }

    public async Task ClearQueueAsync()
    {
        await Task.CompletedTask;
        _globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>()?.Clear();
    }

    public async Task<List<SongDto<SocketVoiceChannel>>> GetQueueAsync()
    {
        await Task.CompletedTask;
        return globalStore.Get<Queue<SongDto<SocketVoiceChannel>>>()?.ToList() ?? [];
    }
}