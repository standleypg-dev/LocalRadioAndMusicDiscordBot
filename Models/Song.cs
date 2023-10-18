using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace radio_discord_bot.Models;

public class Song
{
    public string Url { get; set; } = string.Empty;
     public IVoiceChannel? VoiceChannel { get; set; } = null;
}
