using Application.DTOs;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Tests.Integration;

[Collection("postgres")]
public class StatisticsServiceTests(PostgresFixture fixture)
{
    private static ulong UniqueUserId() =>
        (ulong)Random.Shared.NextInt64(1_000_000, long.MaxValue);

    private static string UniqueUrl() => $"https://youtube.com/watch?v={Guid.NewGuid():N}";

    [Fact]
    public async Task LogSongPlay_Creates_User_Song_And_PlayHistory()
    {
        var userId = UniqueUserId();
        var userName = $"user-{Guid.NewGuid():N}";
        var url = UniqueUrl();
        var title = $"Song {Guid.NewGuid():N}";
        var streamService = Substitute.For<IStreamService>();

        await using (var context = fixture.CreateContext())
        {
            var service = new StatisticsService(context, streamService,
                NullLogger<StatisticsService>.Instance);
            await service.LogSongPlayAsync(userId, userName, "Global Name",
                new SongDtoBase { Url = url, Title = title, UserId = userId });
        }

        await using (var context = fixture.CreateContext())
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Assert.NotNull(user);
            Assert.Equal(userName, user.Username);
            Assert.Equal(1, user.TotalSongsPlayed);

            var song = await context.Songs.FirstOrDefaultAsync(s => s.SourceUrl == url);
            Assert.NotNull(song);
            Assert.Equal(title, song.Title);

            var history = await context.PlayHistory
                .FirstOrDefaultAsync(ph => ph.UserId == userId && ph.SongId == song.Id);
            Assert.NotNull(history);
            Assert.Equal(1, history.TotalPlays);
        }
    }

    [Fact]
    public async Task LogSongPlay_Increments_TotalPlays_For_Repeat_Play()
    {
        var userId = UniqueUserId();
        var userName = $"user-{Guid.NewGuid():N}";
        var url = UniqueUrl();
        var title = $"Song {Guid.NewGuid():N}";
        var streamService = Substitute.For<IStreamService>();
        var songDto = new SongDtoBase { Url = url, Title = title, UserId = userId };

        await using (var context = fixture.CreateContext())
        {
            var service = new StatisticsService(context, streamService,
                NullLogger<StatisticsService>.Instance);
            await service.LogSongPlayAsync(userId, userName, "Global Name", songDto);
        }

        await using (var context = fixture.CreateContext())
        {
            var service = new StatisticsService(context, streamService,
                NullLogger<StatisticsService>.Instance);
            await service.LogSongPlayAsync(userId, userName, "Global Name", songDto);
        }

        await using (var context = fixture.CreateContext())
        {
            var song = await context.Songs.SingleAsync(s => s.SourceUrl == url);
            var histories = await context.PlayHistory
                .Where(ph => ph.UserId == userId && ph.SongId == song.Id)
                .ToListAsync();

            // Deduplicated: one history row whose counter was incremented.
            var history = Assert.Single(histories);
            Assert.Equal(2, history.TotalPlays);

            var user = await context.Users.SingleAsync(u => u.Id == userId);
            Assert.Equal(2, user.TotalSongsPlayed);
        }
    }

    [Fact]
    public async Task LogSongPlay_Uses_Provided_Title_Without_Calling_StreamService()
    {
        var userId = UniqueUserId();
        var url = UniqueUrl();
        var title = $"Radio Station {Guid.NewGuid():N}";
        var streamService = Substitute.For<IStreamService>();

        await using (var context = fixture.CreateContext())
        {
            var service = new StatisticsService(context, streamService,
                NullLogger<StatisticsService>.Instance);
            await service.LogSongPlayAsync(userId, $"user-{Guid.NewGuid():N}", string.Empty,
                new SongDtoBase { Url = url, Title = title, UserId = userId });
        }

        await streamService.DidNotReceiveWithAnyArgs().GetVideoTitleAsync(default!, default);

        await using (var context = fixture.CreateContext())
        {
            var song = await context.Songs.SingleAsync(s => s.SourceUrl == url);
            Assert.Equal(title, song.Title);
        }
    }
}
