using System.Text;
using Discord;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Enums;
using radio_discord_bot.Models.Spotify;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;

namespace radio_discord_bot.Services;

public class SpotifyService(IHttpRequestService httpRequestService, GlobalStore globalStore) : ISpotifyService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    private readonly string _spotifyClientId = Configuration.GetConfiguration<string>("SpotifySettings:ClientId");
    private readonly string _spotifySecret = Configuration.GetConfiguration<string>("SpotifySettings:ClientSecret");
    private const string SpotifyBaseUrl = "https://api.spotify.com";

    // top 5 genres
    private static readonly string[] LaguIbanGenre = ["lagu iban"];

    private Search _tracks = new();
    private Artist _artist = new();

    public async Task GetRecommendationAsync(string songTitle)
    {
        Console.WriteLine("GetRecommendationAsync" + songTitle);
        await CheckAuth();
        await SearchTrackAsync(songTitle);
        await GetArtistAsync();

        var artistId = _tracks.Tracks.Items.FirstOrDefault()?.Artists.FirstOrDefault()?.Id;
        var trackId = _tracks.Tracks.Items.FirstOrDefault()?.Id;
        var genre = _artist.Genres;

        const string url = $"{SpotifyBaseUrl}/v1/recommendations";
        // remove from the list if the genre contains "-"
        genre = genre.Where(x => !x.Contains('-')).ToArray();
        var data = new
        {
            limit = 5,
            market = "MY",
            seed_artists = artistId,
            seed_genres = new StringBuilder().AppendJoin(",", genre.Length > 0 ? genre : LaguIbanGenre).ToString(),
            seed_tracks = trackId
        };
        var response =
            await httpRequestService.GetAsync<Recommendation>(url, data, _globalStore.Get<Auth>()!.AccessToken);
        
        _globalStore.Set<BaseSearch[]>(response.Tracks);
        
        var embed = new EmbedBuilder()
            .WithTitle("You might like these as well:")
            .Build();
        await ReplyToChannel.FollowupEmbebAsync(_globalStore.Get<SocketMessageComponent>()!, MessageComponentGenerator.GenerateComponents(response.Tracks.ToList()), embed);
    }

    #region private methods

    private async Task SearchTrackAsync(string songTitle)
    {
        await CheckAuth();

        const string url = $"{SpotifyBaseUrl}/v1/search";
        var data = new
        {
            q = songTitle,
            type = "track",
            market = "MY",
            limit = 1
        };
        _tracks = await httpRequestService.GetAsync<Search>(url, data, _globalStore.Get<Auth>()!.AccessToken);
    }

    private async Task GetArtistAsync()
    {
        await CheckAuth();

        var artistId = _tracks.Tracks.Items.FirstOrDefault()?.Artists.FirstOrDefault()?.Id;

        var url = $"{SpotifyBaseUrl}/v1/artists/{artistId}";
        _artist = await httpRequestService.GetAsync<Artist>(url, null, _globalStore.Get<Auth>()!.AccessToken);
    }


    private async Task<Auth> GetAuthAsync()
    {
        const string url = $"https://accounts.spotify.com/api/token";
        var data = new
        {
            grant_type = "client_credentials",
            client_id = _spotifyClientId,
            client_secret = _spotifySecret
        };
        var response = await httpRequestService.PostAsync<Auth>(url, data, PostRequestMediaType.FormUrlEncoded);
        return response;
    }

    private async Task CheckAuth()
    {
        if (!_globalStore.TryGet<Auth>(out _))
        {
            _globalStore.Set(await GetAuthAsync());
        }
        else
        {
            var auth = _globalStore.Get<Auth>();
            if (auth?.TimeStamp.AddSeconds(auth.ExpiresIn) < DateTime.Now)
            {
                _globalStore.Set(await GetAuthAsync());
            }
        }
    }

    #endregion
}