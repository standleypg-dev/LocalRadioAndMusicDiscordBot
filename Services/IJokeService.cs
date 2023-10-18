using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace radio_discord_bot.Services;

public interface IJokeService
{
    Task Start();
    Task EnableJoke();
    Task DisableJoke();
}
