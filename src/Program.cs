using Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using radio_discord_bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDiscordServices();
builder.Services.AddHostedService<DiscordBot>();

builder.Services.AddDbContext<DiscordBotContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DiscordBotContext>();

    try
    {
        // Ensure the database is created
        await context.Database.EnsureCreatedAsync();

        // Apply pending migrations (if any)
        await context.Database.MigrateAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == "42P07")
    {
        // 42P07 = "duplicate_table" in PostgreSQL
        Console.WriteLine("Table already exists. Skipping creation.");
    }
    catch (Exception ex)
    {
        // Log or handle other unexpected issues
        Console.WriteLine($"Database migration failed: {ex.Message}");
    }
}


app.Run();