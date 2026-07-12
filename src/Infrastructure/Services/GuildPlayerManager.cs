using System.Collections.Concurrent;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Components backing one guild's <see cref="GuildPlayer"/>. The optional disposable
/// (the guild's FFmpeg process service in production) is disposed when the manager
/// shuts down.
/// </summary>
public sealed record GuildPlayerComponents(
    IMusicQueueService Queue,
    INetCordAudioPlayerService AudioPlayer,
    IDisposable? Disposable = null);

/// <summary>
/// Owns one <see cref="GuildPlayer"/> per guild so multiple servers can play music
/// concurrently. Players (queue + consumer loop + audio player + FFmpeg service) are
/// created lazily on the first enqueue for a guild and kept for the process lifetime;
/// all loops are cancelled and per-guild components disposed on host shutdown.
/// </summary>
public sealed class GuildPlayerManager(
    ILoggerFactory loggerFactory,
    Func<ulong, GuildPlayerComponents> componentFactory)
    : BackgroundService, IGuildMusicService
{
    private readonly ILogger<GuildPlayerManager> _logger = loggerFactory.CreateLogger<GuildPlayerManager>();
    private readonly ConcurrentDictionary<ulong, GuildPlayer> _players = new();
    private readonly List<Task> _loops = [];
    private readonly List<IDisposable> _disposables = [];
    private readonly Lock _createLock = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private bool _disposed;

    public void Enqueue<T>(ulong guildId, PlayRequest<T> request) =>
        GetOrCreatePlayer(guildId).Queue.Enqueue(request);

    public PlayRequest? GetNowPlaying(ulong guildId) =>
        _players.TryGetValue(guildId, out var player) ? player.Queue.NowPlaying : null;

    public PlayRequest[] GetAllRequests(ulong guildId) =>
        _players.TryGetValue(guildId, out var player) ? player.Queue.GetAllRequests() : [];

    public int GetQueueCount(ulong guildId) =>
        _players.TryGetValue(guildId, out var player) ? player.Queue.Count : 0;

    public void Rewind(ulong guildId)
    {
        if (_players.TryGetValue(guildId, out var player))
        {
            player.Queue.Rewind();
        }
    }

    public void Skip(ulong guildId)
    {
        if (_players.TryGetValue(guildId, out var player))
        {
            player.Skip();
        }
    }

    public void Stop(ulong guildId)
    {
        if (_players.TryGetValue(guildId, out var player))
        {
            player.Stop();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Players run on their own tracked loop tasks; this just parks until shutdown.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down.
        }

        await _stoppingCts.CancelAsync();

        Task[] loops;
        lock (_createLock)
        {
            loops = _loops.ToArray();
        }

        try
        {
            await Task.WhenAll(loops).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timed out waiting for guild player loops to stop");
        }

        DisposeComponents();
    }

    private GuildPlayer GetOrCreatePlayer(ulong guildId)
    {
        if (_players.TryGetValue(guildId, out var existing))
        {
            return existing;
        }

        // Plain lock instead of GetOrAdd: the factory starts a consumer loop, so it must
        // run exactly once per guild.
        lock (_createLock)
        {
            if (_players.TryGetValue(guildId, out existing))
            {
                return existing;
            }

            var components = componentFactory(guildId);
            var player = new GuildPlayer(components.Queue, components.AudioPlayer,
                loggerFactory.CreateLogger($"{typeof(GuildPlayer).FullName}[{guildId}]"));

            _loops.Add(RunPlayerLoopAsync(player, guildId));
            if (components.Disposable is not null)
            {
                _disposables.Add(components.Disposable);
            }

            _players[guildId] = player;
            _logger.LogInformation("Created music player for guild {GuildId}", guildId);
            return player;
        }
    }

    private async Task RunPlayerLoopAsync(GuildPlayer player, ulong guildId)
    {
        try
        {
            await player.RunAsync(_stoppingCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Music player loop for guild {GuildId} terminated unexpectedly", guildId);
        }
    }

    private void DisposeComponents()
    {
        IDisposable[] disposables;
        lock (_createLock)
        {
            disposables = _disposables.ToArray();
            _disposables.Clear();
        }

        foreach (var disposable in disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing guild player component");
            }
        }
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _stoppingCts.Cancel();
            _stoppingCts.Dispose();
            DisposeComponents();
        }

        base.Dispose();
    }
}
