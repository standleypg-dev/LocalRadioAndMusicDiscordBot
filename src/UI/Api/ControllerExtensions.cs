using Application.Interfaces.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Api;

public static class ControllerExtensions
{
    public static void AddApiController(this WebApplication app)
    {
        app.MapGet("/api/statistics-all",
                async (IStatisticsService statisticsService) =>
                    await statisticsService.GetAllSongsAsync())
            .WithName("GetStatisticsAll");

        app.MapGet("/api/users",
                async (IUserService userService) => await userService.GetAllUsersAsync())
            .WithName("GetAllUsers");

        app.MapGet("/api/radio-sources",
                async (IRadioSourceService radioSourceService) =>
                    await radioSourceService.GetAllRadioSourcesAsync())
            .RequireAuthorization()
            .WithName("GetAllRadioSources");

        app.MapPut("/api/radio-sources/{id:guid}",
                async (IRadioSourceService radioSourceService, Guid id, [FromBody] UpdateRadioSourceRequest request) =>
                {
                    await radioSourceService.UpdateRadioSourceUrlAsync(id, request.NewSourceUrl, request.IsActive);
                    return Results.NoContent();
                })
            .RequireAuthorization()
            .WithName("UpdateRadioSourceUrl");

        app.MapGet("/api/radio-sources/{id:guid}",
                async (IRadioSourceService radioSourceService, Guid id) =>
                {
                    var radioSource = await radioSourceService.GetRadioSourceByIdAsync(id);
                    return Results.Ok(radioSource);
                })
            .RequireAuthorization()
            .WithName("GetRadioSourceById");

        app.MapPost("/api/radio-sources/add",
                async (IRadioSourceService radioSourceService, [FromBody] AddRadioSourceRequest request) =>
                {
                    try
                    {
                        var id = await radioSourceService.AddRadioSourceAsync(request.Name, request.SourceUrl);
                        var result = await radioSourceService.GetRadioSourceByIdAsync(id);
                        return Results.Created($"/api/radio-sources/{id}", result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }
                    catch (Exception)
                    {
                        return Results.Problem("An unexpected error occurred.");
                    }
                })
            .RequireAuthorization()
            .WithName("AddRadioSource");


        app.MapDelete("/api/radio-sources/{id:guid}",
                async (IRadioSourceService radioSourceService, Guid id) =>
                {
                    await radioSourceService.DeleteRadioSourceAsync(id);
                    return Results.NoContent();
                })
            .RequireAuthorization()
            .WithName("DeleteRadioSource");

        app.MapPost("/api/login",
                async (IUserService userService, IConfiguration configuration, IJwtTokenGenerator tokenGenerator,
                    [FromBody] LoginRequest request) =>
                {
                    try
                    {
                        var user = await userService.GetUserByUsernameAsync(request.Username);
                        // Note: In a real application, you would hash the password and compare it securely.
                        var password = configuration.GetValue<string>("JwtSettings:InternalPassword");
                        if (user == null || password != request.Password)
                        {
                            throw new UnauthorizedAccessException("Invalid username or password.");
                        }

                        var token = tokenGenerator.GenerateToken(request);
                        return Results.Ok(new { token });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return Results.Unauthorized();
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                })
            .AllowAnonymous()
            .WithName("Login");
        
        app.MapGet("/api/auth/validate-token", (HttpContext context) =>
            {
                // Check if user is authenticated (JWT middleware already validated the token)
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    return Results.Ok(new { 
                        valid = true, 
                        username = context.User.Identity.Name,
                        expires = context.User.FindFirst("exp")?.Value
                    });
                }
                
                return Results.Unauthorized();
            })
            .RequireAuthorization()
            .WithName("ValidateToken");
    }
}