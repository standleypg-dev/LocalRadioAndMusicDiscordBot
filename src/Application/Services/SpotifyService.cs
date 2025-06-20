using System.Text;
using Application.Configs;
using Application.DTOs.Spotify;
using Application.Interfaces.Services;
using Application.Store;
using Domain.Common.Enums;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public class SpotifyService(IHttpRequestService httpRequestService, GlobalStore globalStore, IConfiguration configuration) : ISpotifyService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    private readonly string? _spotifyClientId = configuration.GetConfiguration<string>("SpotifySettings:ClientId");
    private readonly string? _spotifySecret = configuration.GetConfiguration<string>("SpotifySettings:ClientSecret");
    private const string SpotifyBaseUrl = "https://api.spotify.com";

    // top 5 genres
    private static readonly string[] LaguIbanGenre = ["lagu iban"];

    private SearchDto _tracks = new();
    private ArtistDto _artistDto = new();

    public async Task GetRecommendationAsync(string songTitle)
    {
        Console.WriteLine("GetRecommendationAsync " + songTitle);
        await CheckAuth();
        await SearchTrackAsync(songTitle);
        await GetArtistAsync();

        var artistId = _tracks.Tracks.Items.FirstOrDefault()?.Artists.FirstOrDefault()?.Id;
        var trackId = _tracks.Tracks.Items.FirstOrDefault()?.Id;
        var genre = _artistDto.Genres;

        const string url = $"{SpotifyBaseUrl}/v1/recommendations";
        // remove from the list if the genre contains "-"
        genre = genre.Where(x => !x.Contains('-')).ToArray();
        var data = new
        {
            limit = 10,
            market = "MY",
            seed_artists = artistId,
            seed_genres = new StringBuilder().AppendJoin(",", genre.Length > 0 ? genre : LaguIbanGenre).ToString(),
            seed_tracks = trackId
        };
        var response =
            await httpRequestService.GetAsync<RecommendationDto>(url, data, _globalStore.Get<AuthDto>()!.AccessToken);
        
        _globalStore.Set(response.Tracks);
        
       
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
        _tracks = await httpRequestService.GetAsync<SearchDto>(url, data, _globalStore.Get<AuthDto>()!.AccessToken);
    }

    private async Task GetArtistAsync()
    {
        await CheckAuth();

        var artistId = _tracks.Tracks.Items.FirstOrDefault()?.Artists.FirstOrDefault()?.Id;

        var url = $"{SpotifyBaseUrl}/v1/artists/{artistId}";
        _artistDto = await httpRequestService.GetAsync<ArtistDto>(url, null, _globalStore.Get<AuthDto>()!.AccessToken);
    }


    private async Task<AuthDto> GetAuthAsync()
    {
        const string url = $"https://accounts.spotify.com/api/token";
        var data = new
        {
            grant_type = "client_credentials",
            client_id = _spotifyClientId,
            client_secret = _spotifySecret
        };
        var response = await httpRequestService.PostAsync<AuthDto>(url, data, PostRequestMediaType.FormUrlEncoded);
        return response;
    }

    private async Task CheckAuth()
    {
        if (!_globalStore.TryGet<AuthDto>(out _))
        {
            _globalStore.Set(await GetAuthAsync());
        }
        else
        {
            var auth = _globalStore.Get<AuthDto>();
            if (auth?.TimeStamp.AddSeconds(auth.ExpiresIn) < DateTimeOffset.UtcNow)
            {
                _globalStore.Set(await GetAuthAsync());
            }
        }
    }

    #endregion
}