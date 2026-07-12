using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Thin adapter translating Skip/Stop events into player controller signals.
/// Playback itself is driven by <see cref="MusicPlayerBackgroundService"/>.
/// </summary>
public class PlayerHandler(IMusicPlayerController playerController, ILogger<PlayerHandler> logger)
    : IEventHandler<EventType.Skip>, IEventHandler<EventType.Stop>
{
    public void Handle(EventType.Skip @event)
    {
        logger.LogInformation("Skip event received - cancelling current track");
        playerController.Skip();
    }

    public void Handle(EventType.Stop @event)
    {
        logger.LogInformation("Stop event received - clearing queue and stopping playback");
        playerController.Stop();
    }
}
