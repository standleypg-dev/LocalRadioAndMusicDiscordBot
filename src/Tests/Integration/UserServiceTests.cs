using Domain.Entities;
using Infrastructure.Services;
using Xunit;

namespace Tests.Integration;

[Collection("postgres")]
public class UserServiceTests(PostgresFixture fixture)
{
    private static ulong UniqueUserId() =>
        (ulong)Random.Shared.NextInt64(1_000_000, long.MaxValue);

    [Fact]
    public async Task GetAllUsers_Returns_Null_LastPlayed_For_User_With_No_History()
    {
        var username = $"user-{Guid.NewGuid():N}";

        await using (var context = fixture.CreateContext())
        {
            context.Users.Add(User.Create(UniqueUserId(), username, "No History"));
            await context.SaveChangesAsync();
        }

        await using (var freshContext = fixture.CreateContext())
        {
            var service = new UserService(freshContext);
            var users = await service.GetAllUsersAsync();

            var user = users.SingleOrDefault(u => u.Username == username);
            Assert.NotNull(user);
            Assert.Null(user.LastPlayed);
            Assert.Equal(0, user.TotalPlays);
        }
    }

    [Fact]
    public async Task GetAllUsers_Orders_By_TotalPlays_Descending()
    {
        var lightUserName = $"user-{Guid.NewGuid():N}";
        var heavyUserName = $"user-{Guid.NewGuid():N}";

        await using (var context = fixture.CreateContext())
        {
            var lightUser = User.Create(UniqueUserId(), lightUserName, "Light");
            var heavyUser = User.Create(UniqueUserId(), heavyUserName, "Heavy");
            context.Users.AddRange(lightUser, heavyUser);

            var lightSong = Song.Create($"https://youtube.com/watch?v={Guid.NewGuid():N}", "Light Song");
            var heavySong = Song.Create($"https://youtube.com/watch?v={Guid.NewGuid():N}", "Heavy Song");
            context.Songs.AddRange(lightSong, heavySong);
            await context.SaveChangesAsync();

            context.PlayHistory.Add(PlayHistory.Create(DateTimeOffset.UtcNow, lightUser.Id, lightSong.Id));

            var heavyHistory = PlayHistory.Create(DateTimeOffset.UtcNow, heavyUser.Id, heavySong.Id);
            PlayHistory.UpdateTotalPlays(heavyHistory);
            PlayHistory.UpdateTotalPlays(heavyHistory);
            context.PlayHistory.Add(heavyHistory);

            await context.SaveChangesAsync();
        }

        await using (var freshContext = fixture.CreateContext())
        {
            var service = new UserService(freshContext);
            var users = (await service.GetAllUsersAsync()).ToList();

            var heavyIndex = users.FindIndex(u => u.Username == heavyUserName);
            var lightIndex = users.FindIndex(u => u.Username == lightUserName);

            Assert.True(heavyIndex >= 0);
            Assert.True(lightIndex >= 0);
            Assert.True(heavyIndex < lightIndex,
                "User with more plays should be ordered before user with fewer plays.");

            Assert.Equal(3, users[heavyIndex].TotalPlays);
            Assert.NotNull(users[heavyIndex].LastPlayed);
        }
    }
}
