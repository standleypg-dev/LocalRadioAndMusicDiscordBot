using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace radio_discord_bot.Services;

public interface IInteractionService
{
    Task OnInteractionCreated(SocketInteraction interaction);
}
