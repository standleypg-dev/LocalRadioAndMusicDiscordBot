using Application.DTOs;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord.Services.ComponentInteractions;
using NSubstitute;
using Xunit;

namespace Tests.Unit;

public class GuildPlayerManagerTests
{
    private const ulong GuildA = 1111;
    private const ulong GuildB = 2222;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static PlayRequest<StringMenuInteractionContext> CreateRequest()
    {
        return new PlayRequest<StringMenuInteractionContext>
        {
            Callbacks = _ => Task.CompletedTask
        };
    }

    private static GuildPlayerManager CreateManager(
        Func<ulong, INetCordAudioPlayerService> audioPlayerFactory,
        Action<ulong>? onCreate = null)
    {
        return new GuildPlayerManager(NullLoggerFactory.Instance, guildId =>
        {
            onCreate?.Invoke(guildId);
            return new GuildPlayerComponents(new MusicQueueService(), audioPlayerFactory(guildId));
        });
    }

    /// <summary>
    /// An audio player whose track signals when it starts and then plays until it is
    /// either released (Completed) or its token is cancelled (Skipped).
    /// </summary>
    private static INetCordAudioPlayerService CreateBlockingAudioPlayer(
        TaskCompletionSource started, TaskCompletionSource release)
    {
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                started.TrySetResult();
                var cancelled = new TaskCompletionSource();
                await using var registration = token.Register(() => cancelled.TrySetResult());
                var finished = await Task.WhenAny(release.Task, cancelled.Task).WaitAsync(Timeout);
                return finished == cancelled.Task ? TrackPlayResult.Skipped : TrackPlayResult.Completed;
            });
        return audioPlayer;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > Timeout.TotalMilliseconds)
            {
                throw new TimeoutException($"Condition not met within timeout: {description}");
            }

            await Task.Delay(25);
        }
    }

    [Fact]
    public async Task Tracks_In_Different_Guilds_Play_Concurrently()
    {
        var startedA = new TaskCompletionSource();
        var releaseA = new TaskCompletionSource();
        var startedB = new TaskCompletionSource();
        var releaseB = new TaskCompletionSource();

        using var manager = CreateManager(guildId => guildId == GuildA
            ? CreateBlockingAudioPlayer(startedA, releaseA)
            : CreateBlockingAudioPlayer(startedB, releaseB));
        await manager.StartAsync(CancellationToken.None);
        try
        {
            manager.Enqueue(GuildA, CreateRequest());
            await startedA.Task.WaitAsync(Timeout);

            // Guild A's track is still held open; guild B's track must start anyway.
            manager.Enqueue(GuildB, CreateRequest());
            await startedB.Task.WaitAsync(Timeout);

            Assert.NotNull(manager.GetNowPlaying(GuildA));
            Assert.NotNull(manager.GetNowPlaying(GuildB));

            releaseA.TrySetResult();
            releaseB.TrySetResult();
        }
        finally
        {
            await manager.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Skip_Cancels_Only_That_Guilds_Track()
    {
        var startedA = new TaskCompletionSource();
        var releaseA = new TaskCompletionSource();
        var startedB = new TaskCompletionSource();
        var releaseB = new TaskCompletionSource();

        using var manager = CreateManager(guildId => guildId == GuildA
            ? CreateBlockingAudioPlayer(startedA, releaseA)
            : CreateBlockingAudioPlayer(startedB, releaseB));
        await manager.StartAsync(CancellationToken.None);
        try
        {
            manager.Enqueue(GuildA, CreateRequest());
            manager.Enqueue(GuildB, CreateRequest());
            await startedA.Task.WaitAsync(Timeout);
            await startedB.Task.WaitAsync(Timeout);

            manager.Skip(GuildA);

            await WaitUntilAsync(() => manager.GetNowPlaying(GuildA) is null, "guild A track skipped");
            Assert.NotNull(manager.GetNowPlaying(GuildB));

            releaseB.TrySetResult();
        }
        finally
        {
            await manager.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Stop_Clears_Only_That_Guilds_Queue()
    {
        var startedA = new TaskCompletionSource();
        var releaseA = new TaskCompletionSource();
        var startedB = new TaskCompletionSource();
        var releaseB = new TaskCompletionSource();

        using var manager = CreateManager(guildId => guildId == GuildA
            ? CreateBlockingAudioPlayer(startedA, releaseA)
            : CreateBlockingAudioPlayer(startedB, releaseB));
        await manager.StartAsync(CancellationToken.None);
        try
        {
            manager.Enqueue(GuildA, CreateRequest());
            manager.Enqueue(GuildA, CreateRequest());
            manager.Enqueue(GuildB, CreateRequest());
            manager.Enqueue(GuildB, CreateRequest());
            await startedA.Task.WaitAsync(Timeout);
            await startedB.Task.WaitAsync(Timeout);

            manager.Stop(GuildA);

            await WaitUntilAsync(
                () => manager.GetNowPlaying(GuildA) is null && manager.GetQueueCount(GuildA) == 0,
                "guild A stopped and cleared");

            // Guild B is untouched: still playing its first track with one pending.
            Assert.NotNull(manager.GetNowPlaying(GuildB));
            Assert.Equal(1, manager.GetQueueCount(GuildB));

            releaseB.TrySetResult();
        }
        finally
        {
            await manager.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void Operations_On_Unknown_Guild_Are_NoOps()
    {
        using var manager = CreateManager(_ => Substitute.For<INetCordAudioPlayerService>());

        Assert.Null(manager.GetNowPlaying(GuildA));
        Assert.Empty(manager.GetAllRequests(GuildA));
        Assert.Equal(0, manager.GetQueueCount(GuildA));

        // None of these should throw or create a player.
        manager.Skip(GuildA);
        manager.Stop(GuildA);
        manager.Rewind(GuildA);

        Assert.Null(manager.GetNowPlaying(GuildA));
    }

    [Fact]
    public async Task Same_Guild_Reuses_The_Same_Player()
    {
        var factoryCalls = 0;
        using var manager = CreateManager(
            _ => Substitute.For<INetCordAudioPlayerService>(),
            _ => Interlocked.Increment(ref factoryCalls));
        await manager.StartAsync(CancellationToken.None);
        try
        {
            manager.Enqueue(GuildA, CreateRequest());
            manager.Enqueue(GuildA, CreateRequest());
            manager.Enqueue(GuildB, CreateRequest());

            Assert.Equal(2, Volatile.Read(ref factoryCalls));
        }
        finally
        {
            await manager.StopAsync(CancellationToken.None);
        }
    }
}
