using Application.DTOs;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;

namespace Api;

public static class ControllerExtensions
{
    public static void AddApiController(this WebApplication app)
    {
        app.MapGet("/statistics-all",
            async (IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>> statisticsService) =>
                await statisticsService.GetAllSongsAsync()).WithName("GetStatisticsAll");
    }
}