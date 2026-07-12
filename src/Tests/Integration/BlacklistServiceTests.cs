using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Integration;

[Collection("postgres")]
public class BlacklistServiceTests(PostgresFixture fixture)
{
    private static string UniqueUrl() => $"https://youtube.com/watch?v={Guid.NewGuid():N}";

    private async Task<Song> SeedSongAsync(string url, string title, bool blacklisted = false)
    {
        await using var context = fixture.CreateContext();
        var song = Song.Create(url, title);
        if (blacklisted)
        {
            Song.MarkAsBlacklisted(song, true);
        }

        context.Songs.Add(song);
        await context.SaveChangesAsync();
        return song;
    }

    [Fact]
    public async Task AddToBlacklist_Marks_Existing_Song_And_Returns_True()
    {
        var url = UniqueUrl();
        await SeedSongAsync(url, $"Song {Guid.NewGuid():N}");

        await using (var context = fixture.CreateContext())
        {
            var service = new BlacklistService(context);
            Assert.True(await service.AddToBlacklistAsync(url));
        }

        await using (var context = fixture.CreateContext())
        {
            var service = new BlacklistService(context);
            Assert.True(await service.IsBlacklistedAsync(url));
        }
    }

    [Fact]
    public async Task AddToBlacklist_Returns_False_When_Song_Not_Found()
    {
        await using var context = fixture.CreateContext();
        var service = new BlacklistService(context);

        Assert.False(await service.AddToBlacklistAsync(UniqueUrl()));
    }

    [Fact]
    public async Task RemoveFromBlacklist_By_Partial_Title_Returns_True()
    {
        var url = UniqueUrl();
        var marker = Guid.NewGuid().ToString("N");
        await SeedSongAsync(url, $"Some Great Song {marker}", blacklisted: true);

        await using (var context = fixture.CreateContext())
        {
            var service = new BlacklistService(context);
            Assert.True(await service.RemoveFromBlacklistAsync(marker));
        }

        await using (var context = fixture.CreateContext())
        {
            var service = new BlacklistService(context);
            Assert.False(await service.IsBlacklistedAsync(url));
        }
    }

    [Fact]
    public async Task RemoveFromBlacklist_Returns_False_When_Not_Found()
    {
        await using var context = fixture.CreateContext();
        var service = new BlacklistService(context);

        Assert.False(await service.RemoveFromBlacklistAsync(Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task RemoveFromBlacklist_Treats_Like_Wildcards_As_Literals()
    {
        var url = UniqueUrl();
        var marker = Guid.NewGuid().ToString("N");
        await SeedSongAsync(url, $"Wildcard {marker}", blacklisted: true);

        await using var context = fixture.CreateContext();
        var service = new BlacklistService(context);

        // "%" would match everything if it were not escaped; it must not match this song.
        Assert.False(await service.RemoveFromBlacklistAsync($"{marker}%extra"));
        Assert.True(await service.IsBlacklistedAsync(url));
    }

    [Fact]
    public async Task IsBlacklisted_Reflects_Flag()
    {
        var url = UniqueUrl();
        await SeedSongAsync(url, $"Song {Guid.NewGuid():N}");

        await using var context = fixture.CreateContext();
        var service = new BlacklistService(context);

        Assert.False(await service.IsBlacklistedAsync(url));

        var song = await context.Songs.FirstAsync(s => s.SourceUrl == url);
        Song.MarkAsBlacklisted(song, true);
        await context.SaveChangesAsync();

        Assert.True(await service.IsBlacklistedAsync(url));
    }
}
