using Application.DTOs;
using Application.Interfaces.Services;
using Discord.WebSocket;
using Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class ControllerExtensions
{
    public static void AddApiController(this WebApplication app)
    {
        app.MapGet("/api/statistics-all",
            async (IStatisticsService<SocketUser, SongDto<SocketVoiceChannel>> statisticsService) =>
                await statisticsService.GetAllSongsAsync())
            .WithName("GetStatisticsAll");

        app.MapGet("/api/users",
            async (IUserService userService) => await userService.GetAllUsersAsync())
            .WithName("GetAllUsers");
        
        app.MapGet("/api/radio-sources",
            async (IRadioSourceService radioSourceService) => 
                await radioSourceService.GetAllRadioSourcesAsync())
            .WithName("GetAllRadioSources");
        
        app.MapPut("/api/radio-sources/{id:guid}",
            async (IRadioSourceService radioSourceService, Guid id, [FromBody] UpdateRadioSourceRequest request) =>
            {
                await radioSourceService.UpdateRadioSourceUrlAsync(id, request.NewSourceUrl, request.IsActive);
                return Results.NoContent();
            })
            .WithName("UpdateRadioSourceUrl");
        
        app.MapGet("/api/radio-sources/{id:guid}",
            async (IRadioSourceService radioSourceService, Guid id) =>
            {
                var radioSource = await radioSourceService.GetRadioSourceByIdAsync(id);
                return Results.Ok(radioSource);
            })
            .WithName("GetRadioSourceById");
        
        app.MapPost("/api/radio-sources/add",
            async (IRadioSourceService radioSourceService, [FromBody] AddRadioSourceRequest request) =>
            {
                var id = await radioSourceService.AddRadioSourceAsync(request.Name, request.SourceUrl);
                var result = await radioSourceService.GetRadioSourceByIdAsync(id);
                return Results.Created($"/api/radio-sources/{id}", result);
            })
            .WithName("AddRadioSource");
        
        app.MapDelete("/api/radio-sources/{id:guid}",
            async (IRadioSourceService radioSourceService, Guid id) =>
            {
                await radioSourceService.DeleteRadioSourceAsync(id);
                return Results.NoContent();
            })
            .WithName("DeleteRadioSource");
    }
}