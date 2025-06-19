using Data;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

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


app.Run();