using Application.Interfaces.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Api;

public static class ControllerExtensions
{
    extension(WebApplication app)
    {
        public void AddApiController()
        {
            app.MapGet("/api/statistics-all",
                    async (IStatisticsService statisticsService) =>
                        await statisticsService.GetAllSongsAsync())
                .WithName("GetStatisticsAll");

            app.MapGet("/api/statistics-today",
                    async (IStatisticsService statisticsService, int? limit) =>
                        await statisticsService.GetTopSongsAsync(true, limit ?? 100))
                .WithName("GetStatisticsToday");

            app.MapGet("/api/users",
                    async (IUserService userService) => await userService.GetAllUsersAsync())
                .WithName("GetAllUsers");

            app.MapGet("/api/radio-sources",
                    async (IRadioSourceService radioSourceService, CancellationToken cancellationToken) =>
                        await radioSourceService.GetAllRadioSourcesAsync(cancellationToken))
                .RequireAuthorization()
                .WithName("GetAllRadioSources");

            app.MapPut("/api/radio-sources/{id:guid}",
                    async (IRadioSourceService radioSourceService, Guid id, [FromBody] UpdateRadioSourceRequest request,
                        CancellationToken cancellationToken) =>
                    {
                        try
                        {
                            await radioSourceService.UpdateRadioSourceUrlAsync(id, request.Name, request.NewSourceUrl,
                                request.IsActive, cancellationToken);
                            return Results.NoContent();
                        }
                        catch (KeyNotFoundException)
                        {
                            return Results.NotFound();
                        }
                        catch (ArgumentException ex)
                        {
                            return Results.BadRequest(new { error = ex.Message });
                        }
                    })
                .RequireAuthorization()
                .WithName("UpdateRadioSourceUrl");

            app.MapGet("/api/radio-sources/{id:guid}",
                    async (IRadioSourceService radioSourceService, Guid id, CancellationToken cancellationToken) =>
                    {
                        try
                        {
                            var radioSource = await radioSourceService.GetRadioSourceByIdAsync(id, cancellationToken);
                            return Results.Ok(radioSource);
                        }
                        catch (KeyNotFoundException)
                        {
                            return Results.NotFound();
                        }
                    })
                .RequireAuthorization()
                .WithName("GetRadioSourceById");

            app.MapPost("/api/radio-sources/add",
                    async (IRadioSourceService radioSourceService, [FromBody] AddRadioSourceRequest request,
                        CancellationToken cancellationToken) =>
                    {
                        try
                        {
                            var id = await radioSourceService.AddRadioSourceAsync(request.Name, request.SourceUrl,
                                cancellationToken);
                            var result = await radioSourceService.GetRadioSourceByIdAsync(id, cancellationToken);
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
                    async (IRadioSourceService radioSourceService, Guid id, CancellationToken cancellationToken) =>
                    {
                        try
                        {
                            await radioSourceService.DeleteRadioSourceAsync(id, cancellationToken);
                            return Results.NoContent();
                        }
                        catch (KeyNotFoundException)
                        {
                            return Results.NotFound();
                        }
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
                            // Fail closed if the internal password is not configured or the request omits one.
                            if (string.IsNullOrEmpty(password) || user == null || password != request.Password)
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
                        catch (Exception)
                        {
                            return Results.Problem("An unexpected error occurred.");
                        }
                    })
                .AllowAnonymous()
                .WithName("Login");

            app.MapGet("/api/auth/validate-token", (HttpContext context) =>
                {
                    // Check if user is authenticated (JWT middleware already validated the token)
                    if (context.User.Identity?.IsAuthenticated == true)
                    {
                        return Results.Ok(new
                        {
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
}