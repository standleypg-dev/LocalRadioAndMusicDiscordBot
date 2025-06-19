using Discord;
using Domain.Common.Enums;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;

namespace Infrastructure.Utils;

public interface ISearchGenerator
{
    Task<(Embed?, IReadOnlyList<VideoSearchResult>?)> SearchAsync(string command, YtSearchCollection ytSearchCollection = YtSearchCollection.FirstFive);
}

public class SearchGenerator(IServiceProvider serviceProvider) : ISearchGenerator
{
    public async Task<(Embed?, IReadOnlyList<VideoSearchResult>?)> SearchAsync(string command, YtSearchCollection ytSearchCollection = YtSearchCollection.FirstFive)
    {
        IReadOnlyList<VideoSearchResult>? videos;
        string title;
        System.Console.WriteLine(ytSearchCollection);
        using var scope = serviceProvider.CreateScope();
        var youtubeClient = scope.ServiceProvider.GetRequiredService<YoutubeClient>();
        switch (ytSearchCollection)
        {
            case YtSearchCollection.FirstFive:
                videos = await youtubeClient.Search.GetVideosAsync(command).CollectAsync(5);
                title = "Choose your song";
                break;
            case YtSearchCollection.Random:
                var random = new Random();
                videos = await youtubeClient.Search.GetVideosAsync(command).Skip(10).OrderBy(_ => random.Next()).CollectAsync(5);
                title = "You might like these songs";
                break;
            default:
                title = string.Empty;
                videos = [];
                break;
        }
        System.Console.WriteLine("Total videos: " + videos.Count);

        var embed = new EmbedBuilder()
            .WithTitle($"{title}")
            .Build();

        return (embed, videos);
    }
}
