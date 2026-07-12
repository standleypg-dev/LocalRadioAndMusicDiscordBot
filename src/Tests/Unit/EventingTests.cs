using Application.Eventing;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Domain.Events;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Tests.Unit;

public class EventingTests
{
    [Fact]
    public void AddEventing_Registers_PlayerHandler_As_Sync_Handler_For_Skip_And_Stop()
    {
        var services = new ServiceCollection();
        services.AddEventing(
            typeof(Application.AssemblyMarker).Assembly,
            typeof(Infrastructure.Services.AssemblyMarker).Assembly);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<HandlerRegistry>();

        Assert.Contains(typeof(PlayerHandler), registry.GetSyncHandlers(typeof(EventType.Skip)));
        Assert.Contains(typeof(PlayerHandler), registry.GetSyncHandlers(typeof(EventType.Stop)));

        // The Play event is no longer handled; enqueueing wakes the background consumer directly.
        Assert.Empty(registry.GetSyncHandlers(typeof(EventType.Play)));
        Assert.Empty(registry.GetAsyncHandlers(typeof(EventType.Play)));
    }

    [Fact]
    public void EventDispatcher_Dispatch_Invokes_Registered_Sync_Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EventRecorder>();
        services.AddEventing(typeof(EventingTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        dispatcher.Dispatch(new TestEvent());
        dispatcher.Dispatch(new TestEvent());

        var recorder = provider.GetRequiredService<EventRecorder>();
        Assert.Equal(2, recorder.Count);
    }

    [Fact]
    public void PlayerHandler_Forwards_Guild_Scoped_Skip_And_Stop()
    {
        var guildMusicService = Substitute.For<IGuildMusicService>();
        var handler = new PlayerHandler(guildMusicService, NullLogger<PlayerHandler>.Instance);

        handler.Handle(new EventType.Skip(42));
        handler.Handle(new EventType.Stop(99));

        guildMusicService.Received(1).Skip(42);
        guildMusicService.Received(1).Stop(99);
    }
}

public sealed record TestEvent : IEvent;

public sealed class EventRecorder
{
    public int Count;
}

public sealed class TestEventHandler(EventRecorder recorder) : IEventHandler<TestEvent>
{
    public void Handle(TestEvent @event)
    {
        Interlocked.Increment(ref recorder.Count);
    }
}
