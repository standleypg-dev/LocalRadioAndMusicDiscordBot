using Application.DTOs;
using Application.DTOs.Spotify;
using Application.Interfaces.Services;
using Application.Store;
using Discord.WebSocket;
using Infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Infrastructure.Services;

public class QueueService(
    GlobalStore globalStore,
    ILogger<QueueService> logger,
    IServiceProvider serviceProvider) : IQueueService<SongDto<SocketVoiceChannel>>
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    public event OnSongAdded? SongAdded;

    public async Task AddSongAsync(SongDto<SocketVoiceChannel> songDto, bool followup = true)
    {
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        var youtubeService = scope.ServiceProvider.GetRequiredService<IYoutubeService>();
        try
        {
            if (!Uri.TryCreate(songDto.Url, UriKind.Absolute, out _))
            {
                var item = _globalStore.Get<Items[]>()?.ToList().Find(x => x.Id == songDto.Url) ??
                           throw new ArgumentException("Invalid song URL or ID.");
                var videos = await youtubeClient.Search.GetVideosAsync(item.Name).CollectAsync(1);
                songDto.Url = videos[0].Url;
            }

            string songTitle = songDto.Title;
            if (songTitle is null or "")
            {
                songTitle = await youtubeService.GetVideoTitleAsync(songDto.Url);
                songDto.Title = songTitle;
            }

            if (_globalStore.TryGet<SocketMessageComponent>(out var component) && followup)
            {
                await component.FollowupAsync(
                    $"Added {songTitle} to the queue.");
            }

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
            logger.LogError(ex, "Error adding song to queue: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to add song to queue.", ex);
        }
    }

    public async Task SkipSongAsync()
    {
        if (_globalStore.TryGet<Queue<SongDto<SocketVoiceChannel>>>(out var queue) && queue.Count > 0)
        {
            queue.Dequeue();
        }
        else if (_globalStore.TryGet<SocketMessageComponent>(out var component))
        {
            await component.FollowupAsync("There are no songs in the queue.");
        }

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