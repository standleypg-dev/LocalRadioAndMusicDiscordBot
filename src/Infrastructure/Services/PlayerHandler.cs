using Application.Interfaces.Services;
using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Thin adapter translating guild-scoped Skip/Stop events into player signals.
/// Playback itself is driven by the per-guild <see cref="GuildPlayer"/> loops owned
/// by <see cref="GuildPlayerManager"/>.
/// </summary>
public class PlayerHandler(IGuildMusicService guildMusicService, ILogger<PlayerHandler> logger)
    : IEventHandler<EventType.Skip>, IEventHandler<EventType.Stop>
{
    public void Handle(EventType.Skip @event)
    {
        logger.LogInformation("Skip event received for guild {GuildId} - cancelling current track", @event.GuildId);
        guildMusicService.Skip(@event.GuildId);
    }

    public void Handle(EventType.Stop @event)
    {
        logger.LogInformation("Stop event received for guild {GuildId} - clearing queue and stopping playback",
            @event.GuildId);
        guildMusicService.Stop(@event.GuildId);
    }
}
