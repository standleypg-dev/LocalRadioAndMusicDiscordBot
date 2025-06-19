using Discord.WebSocket;

namespace Infrastructure.Interfaces.Services;

public interface IInteractionService
{
    Task OnInteractionCreated(SocketInteraction interaction);
    Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState);
}
