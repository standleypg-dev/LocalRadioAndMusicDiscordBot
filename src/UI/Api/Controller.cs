using Application.DTOs;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;

namespace Api;

public static class Main
{
    public static void AddApiController(this WebApplication app)
    {
        app.MapGet("/statistics-all",
            async (IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>> statisticsService) =>
                await statisticsService.GetTopSongsAsync()).WithName("GetStatisticsAll");
    }
}