using Application.DTOs;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord.Services.ComponentInteractions;
using NSubstitute;
using Xunit;

namespace Tests.Unit;

public class GuildPlayerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static PlayRequest<StringMenuInteractionContext> CreateRequest(List<string>? messages = null)
    {
        return new PlayRequest<StringMenuInteractionContext>
        {
            Callbacks = message =>
            {
                if (messages is not null)
                {
                    lock (messages)
                    {
                        messages.Add(message);
                    }
                }

                return Task.CompletedTask;
            }
        };
    }

    private static GuildPlayer CreatePlayer(IMusicQueueService queue, INetCordAudioPlayerService audioPlayer)
    {
        return new GuildPlayer(queue, audioPlayer, NullLogger.Instance);
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
    public async Task Plays_Queued_Tracks_Sequentially_In_Order()
    {
        var queue = new MusicQueueService();
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        var played = new List<Guid>();
        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                lock (played)
                {
                    played.Add(callInfo.Arg<PlayRequest>().Id);
                }

                return TrackPlayResult.Completed;
            });

        var first = CreateRequest();
        var second = CreateRequest();
        queue.Enqueue(first);
        queue.Enqueue(second);

        var player = CreatePlayer(queue, audioPlayer);
        using var cts = new CancellationTokenSource();
        var runTask = player.RunAsync(cts.Token);
        try
        {
            await WaitUntilAsync(() =>
            {
                lock (played)
                {
                    return played.Count == 2;
                }
            }, "both tracks played");

            Assert.Equal([first.Id, second.Id], played);
            Assert.Null(queue.NowPlaying);
        }
        finally
        {
            await cts.CancelAsync();
            await runTask;
        }
    }

    [Fact]
    public async Task Failed_Track_Is_Retried_Up_To_Three_Attempts_Then_Dropped()
    {
        var queue = new MusicQueueService();
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        var attempts = 0;
        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref attempts);
                return TrackPlayResult.Failed;
            });

        queue.Enqueue(CreateRequest());

        var player = CreatePlayer(queue, audioPlayer);
        using var cts = new CancellationTokenSource();
        var runTask = player.RunAsync(cts.Token);
        try
        {
            await WaitUntilAsync(() => Volatile.Read(ref attempts) == 3, "three play attempts");
            await WaitUntilAsync(() => queue.NowPlaying is null, "track dropped");

            // Give the loop a moment to prove it does not retry a fourth time.
            await Task.Delay(200);
            Assert.Equal(3, Volatile.Read(ref attempts));
        }
        finally
        {
            await cts.CancelAsync();
            await runTask;
        }
    }

    [Fact]
    public async Task Skip_Cancels_Current_Track_And_Advances_To_Next()
    {
        var queue = new MusicQueueService();
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        var firstStarted = new TaskCompletionSource();
        var playedSecond = new TaskCompletionSource();
        var first = CreateRequest();
        var second = CreateRequest();

        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var request = callInfo.Arg<PlayRequest>();
                var token = callInfo.Arg<CancellationToken>();
                if (request.Id == first.Id)
                {
                    firstStarted.TrySetResult();
                    // Simulate a long track that only ends when cancelled.
                    var cancelled = new TaskCompletionSource();
                    await using var registration = token.Register(() => cancelled.TrySetResult());
                    await cancelled.Task.WaitAsync(Timeout);
                    return TrackPlayResult.Skipped;
                }

                playedSecond.TrySetResult();
                return TrackPlayResult.Completed;
            });

        queue.Enqueue(first);
        queue.Enqueue(second);

        var player = CreatePlayer(queue, audioPlayer);
        using var cts = new CancellationTokenSource();
        var runTask = player.RunAsync(cts.Token);
        try
        {
            await firstStarted.Task.WaitAsync(Timeout);

            player.Skip();

            await playedSecond.Task.WaitAsync(Timeout);
        }
        finally
        {
            await cts.CancelAsync();
            await runTask;
        }
    }

    [Fact]
    public async Task Stop_Clears_Pending_Cancels_Current_And_Disconnects()
    {
        var queue = new MusicQueueService();
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        var firstStarted = new TaskCompletionSource();
        var disconnected = new TaskCompletionSource();
        var first = CreateRequest();
        var second = CreateRequest();

        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                firstStarted.TrySetResult();
                var cancelled = new TaskCompletionSource();
                await using var registration = token.Register(() => cancelled.TrySetResult());
                await cancelled.Task.WaitAsync(Timeout);
                return TrackPlayResult.Skipped;
            });
        audioPlayer.DisconnectAsync().Returns(_ =>
        {
            disconnected.TrySetResult();
            return Task.CompletedTask;
        });

        queue.Enqueue(first);
        queue.Enqueue(second);

        var player = CreatePlayer(queue, audioPlayer);
        using var cts = new CancellationTokenSource();
        var runTask = player.RunAsync(cts.Token);
        try
        {
            await firstStarted.Task.WaitAsync(Timeout);

            player.Stop();

            await disconnected.Task.WaitAsync(Timeout);
            Assert.Equal(0, queue.Count);

            // Only the first track was ever played; the second was cleared.
            await audioPlayer.Received(1).PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            await cts.CancelAsync();
            await runTask;
        }
    }

    [Fact]
    public async Task Disconnects_When_Queue_Empty_After_Last_Track()
    {
        var queue = new MusicQueueService();
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        var disconnected = new TaskCompletionSource();
        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>())
            .Returns(TrackPlayResult.Completed);
        audioPlayer.DisconnectAsync().Returns(_ =>
        {
            disconnected.TrySetResult();
            return Task.CompletedTask;
        });

        var messages = new List<string>();
        queue.Enqueue(CreateRequest(messages));

        var player = CreatePlayer(queue, audioPlayer);
        using var cts = new CancellationTokenSource();
        var runTask = player.RunAsync(cts.Token);
        try
        {
            await disconnected.Task.WaitAsync(Timeout);

            await WaitUntilAsync(() =>
            {
                lock (messages)
                {
                    return messages.Count > 0;
                }
            }, "disconnect message sent");
            Assert.Contains("Disconnected from voice channel", messages[0]);
        }
        finally
        {
            await cts.CancelAsync();
            await runTask;
        }
    }

    [Fact]
    public async Task NotInVoiceChannel_Result_Invokes_Request_Callback_And_Does_Not_Retry()
    {
        var queue = new MusicQueueService();
        var audioPlayer = Substitute.For<INetCordAudioPlayerService>();
        audioPlayer.PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>())
            .Returns(TrackPlayResult.NotInVoiceChannel);

        var messages = new List<string>();
        queue.Enqueue(CreateRequest(messages));

        var player = CreatePlayer(queue, audioPlayer);
        using var cts = new CancellationTokenSource();
        var runTask = player.RunAsync(cts.Token);
        try
        {
            await WaitUntilAsync(() =>
            {
                lock (messages)
                {
                    return messages.Any(m => m.Contains("not connected to any voice channel"));
                }
            }, "not-in-voice-channel message sent");

            await audioPlayer.Received(1).PlayTrackAsync(Arg.Any<PlayRequest>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            await cts.CancelAsync();
            await runTask;
        }
    }
}
