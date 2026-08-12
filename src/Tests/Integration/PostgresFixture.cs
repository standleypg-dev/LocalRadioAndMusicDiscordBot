using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Spins up one PostgreSQL container for the whole integration collection and
/// applies the real EF Core migrations. Tests share the database, so every
/// test must use unique identifiers (URLs, usernames, user ids).
/// Requires a local Docker daemon.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DiscordBotContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DiscordBotContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new DiscordBotContext(options);
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
