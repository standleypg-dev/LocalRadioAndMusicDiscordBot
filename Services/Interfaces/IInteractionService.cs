using Discord.WebSocket;

namespace radio_discord_bot.Services.Interfaces;

public interface IInteractionService
{
    Task OnInteractionCreated(SocketInteraction interaction);
    Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState);
}
