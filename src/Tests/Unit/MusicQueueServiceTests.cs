using Application.DTOs;
using Infrastructure.Services;
using NetCord.Services.ComponentInteractions;
using Xunit;

namespace Tests.Unit;

public class MusicQueueServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static PlayRequest<StringMenuInteractionContext> CreateRequest(string? url = null)
    {
        return new PlayRequest<StringMenuInteractionContext>
        {
            Callbacks = _ => Task.CompletedTask,
            VideoUrl = url
        };
    }

    [Fact]
    public async Task Enqueue_Then_Dequeue_Returns_Items_In_Fifo_Order()
    {
        var queue = new MusicQueueService();
        var first = CreateRequest();
        var second = CreateRequest();
        var third = CreateRequest();

        queue.Enqueue(first);
        queue.Enqueue(second);
        queue.Enqueue(third);

        Assert.Equal(first.Id, (await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None)).Id);
        Assert.Equal(second.Id, (await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None)).Id);
        Assert.Equal(third.Id, (await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None)).Id);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task Dequeue_Waits_Until_Item_Is_Enqueued()
    {
        var queue = new MusicQueueService();

        var dequeueTask = queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None).AsTask();
        await Task.Delay(100);
        Assert.False(dequeueTask.IsCompleted);

        var request = CreateRequest();
        queue.Enqueue(request);

        var dequeued = await dequeueTask.WaitAsync(Timeout);
        Assert.Equal(request.Id, dequeued.Id);
    }

    [Fact]
    public async Task Dequeue_Honors_Cancellation()
    {
        var queue = new MusicQueueService();
        using var cts = new CancellationTokenSource();

        var dequeueTask = queue.DequeueAsync<StringMenuInteractionContext>(cts.Token).AsTask();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeueTask.WaitAsync(Timeout));
    }

    [Fact]
    public async Task Clear_Removes_Pending_Items_And_Stale_Signals_Do_Not_Yield_Items()
    {
        var queue = new MusicQueueService();
        queue.Enqueue(CreateRequest());
        queue.Enqueue(CreateRequest());

        queue.Clear();
        Assert.Equal(0, queue.Count);

        // The two stale signals left behind by Clear must not produce items.
        var afterClear = CreateRequest();
        queue.Enqueue(afterClear);

        var dequeued = await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None)
            .AsTask().WaitAsync(Timeout);
        Assert.Equal(afterClear.Id, dequeued.Id);

        // Nothing is left: the next dequeue must block even though stale signals may remain.
        var blocked = queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None).AsTask();
        await Task.Delay(100);
        Assert.False(blocked.IsCompleted);
    }

    [Fact]
    public void Count_Excludes_NowPlaying_And_GetAllRequests_Puts_NowPlaying_First()
    {
        var queue = new MusicQueueService();
        var playing = CreateRequest();
        var pending = CreateRequest();

        queue.SetNowPlaying(playing);
        queue.Enqueue(pending);

        Assert.Equal(1, queue.Count);
        Assert.Equal(playing.Id, queue.NowPlaying?.Id);

        var all = queue.GetAllRequests();
        Assert.Equal(2, all.Length);
        Assert.Equal(playing.Id, all[0].Id);
        Assert.Equal(pending.Id, all[1].Id);

        queue.SetNowPlaying(null);
        Assert.Null(queue.NowPlaying);
        Assert.Single(queue.GetAllRequests());
    }

    [Fact]
    public async Task Rewind_Inserts_NowPlaying_At_Front_And_Signals()
    {
        var queue = new MusicQueueService();
        var playing = CreateRequest();
        playing.RetryCount = 2;
        var pending = CreateRequest();

        queue.SetNowPlaying(playing);
        queue.Enqueue(pending);

        queue.Rewind();

        // The rewound track is in front of the previously pending one, with retries reset.
        var first = await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None)
            .AsTask().WaitAsync(Timeout);
        Assert.Equal(playing.Id, first.Id);
        Assert.Equal(0, first.RetryCount);

        var second = await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None)
            .AsTask().WaitAsync(Timeout);
        Assert.Equal(pending.Id, second.Id);
    }

    [Fact]
    public void Rewind_Without_NowPlaying_Is_A_NoOp()
    {
        var queue = new MusicQueueService();
        queue.Rewind();
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task Concurrent_Enqueues_Are_All_Dequeued_Exactly_Once()
    {
        var queue = new MusicQueueService();
        const int producers = 4;
        const int itemsPerProducer = 25;
        const int total = producers * itemsPerProducer;

        var produced = new List<Guid>();
        var producerTasks = Enumerable.Range(0, producers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < itemsPerProducer; i++)
            {
                var request = CreateRequest();
                lock (produced)
                {
                    produced.Add(request.Id);
                }

                queue.Enqueue(request);
            }
        })).ToArray();

        var consumed = new List<Guid>();
        var consumerTask = Task.Run(async () =>
        {
            for (var i = 0; i < total; i++)
            {
                var item = await queue.DequeueAsync<StringMenuInteractionContext>(CancellationToken.None);
                consumed.Add(item.Id);
            }
        });

        await Task.WhenAll(producerTasks).WaitAsync(Timeout);
        await consumerTask.WaitAsync(Timeout);

        Assert.Equal(total, consumed.Count);
        Assert.Equal(consumed.Count, consumed.Distinct().Count());
        Assert.Equal(produced.OrderBy(id => id), consumed.OrderBy(id => id));
        Assert.Equal(0, queue.Count);
    }
}
