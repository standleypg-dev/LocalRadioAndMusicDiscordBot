using System.Diagnostics;

namespace radio_discord_bot.Services.Interfaces;

public interface IFfmpegProcessService
{
    Task<Process> CreateStream(string audioUrl, CancellationToken cancellationToken);
}