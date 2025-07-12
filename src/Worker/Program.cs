using Api;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddOpenApi();

builder.Services.AddDiscordServices();
builder.Services.AddHostedService<DiscordBot>();

builder.Services.AddDbContext<DiscordBotContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(policyBuilder =>
{
    policyBuilder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DiscordBotContext>();
    var connectionString = context.Database.GetDbConnection().ConnectionString;
    logger.LogInformation("Using DB: {ConnectionString}", connectionString);

    try
    {
        // Apply pending migrations (if any)
        await context.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        logger.LogInformation("Table already exists. Skipping creation.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
    }
}

app.AddApiController();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

await app.RunAsync("http://0.0.0.0:5000");