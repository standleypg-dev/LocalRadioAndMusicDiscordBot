using Discord;
using Discord.Commands;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Utils;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace radio_discord_bot.Commands;

public class RadioCommand(
    IAudioPlayerService audioPlayer,
    YoutubeClient youtubeClient,
    IJokeService jokeService,
    IQuoteService quoteService,
    IQueueService queueService,
    IYoutubeService youtubeService)
    : ModuleBase<SocketCommandContext>
{
    private const bool IsProd = true;

    [Command(IsProd ? "play" : "playdev")]
    public async Task HelloCommand([Remainder] string command)
    {
        if (command.Equals("radio"))
        {
            MessageComponentGenerator.GenerateComponents(Configuration.GetConfiguration<List<Radio>>("Radios"),
                colInRow: 2);
            var embed = new EmbedBuilder()
                .WithTitle("Choose your favorite radio station:")
                .WithFooter("Powered by RMT & Astro")
                .Build();

            await ReplyAsync(embed: embed,
                components: MessageComponentGenerator.GenerateComponents(
                    Configuration.GetConfiguration<List<Radio>>("Radios"), colInRow: 3));
        }
        else if (Uri.TryCreate(command, UriKind.Absolute, out _))
        {
            var videos =
                await youtubeClient.Search.GetVideosAsync(command)
                    .CollectAsync(5); // Adjust the number of results we want

            var embed = new EmbedBuilder()
                .WithTitle("Click to play or to add to the queue:")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(videos.ToList()));
        }
        else
        {
            var videos =
                await youtubeClient.Search.GetVideosAsync(command)
                    .CollectAsync(5); // Adjust the number of results we want

            var embed = new EmbedBuilder()
                .WithTitle("Choose your song")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(videos.ToList()));
        }
    }

    [Command(IsProd ? "help" : "helpdev")]
    public async Task HelpCommand()
    {
        var helpMessage = Configuration.GetConfiguration<HelpMessage>("HelpMessage");
        var embed = new EmbedBuilder()
            .WithTitle(helpMessage.Title)
            .WithDescription(helpMessage.Description)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command(IsProd ? "stop" : "stopdev")]
    public async Task StopCommand()
    {
        await ReplyAsync("Stopping radio..");
        await audioPlayer.DestroyVoiceChannelAsync();
        await queueService.ClearQueueAsync();
    }

    [Command(IsProd ? "next" : "nextdev")]
    public async Task NextCommand()
    {
        if ((await queueService.GetQueueAsync()).Count == 1)
            await ReplyAsync("No songs in queue. Nothing to next.");
        else
        {
            await ReplyAsync("Playing next song..");
            await audioPlayer.NextSongAsync();
        }
    }

    [Command(IsProd ? "playlist" : "playlistdev")]
    public async Task QueueCommand()
    {
        var songs = await queueService.GetQueueAsync();

        if (songs.Count == 0)
            await ReplyAsync("No songs in queue.");
        else
        {
            await ReplyAsync("Queues: " + Environment.NewLine + string.Join(Environment.NewLine, songs.Select(
                (title, index) =>
                {
                    var isPlayingNowMsg = index == 0 ? "(Playing now)" : "";
                    return $"{index + 1}. {title.Title} {isPlayingNowMsg}";
                })));
        }
    }

    [Command("tell")]
    public async Task TellJoke([Remainder] string command)
    {
        if (command.Equals("joke"))
        {
            await ReplyAsync(await jokeService.GetJokeAsync(), isTTS: true);
        }
    }

    [Command("motivate")]
    public async Task TellQuote([Remainder] string command)
    {
        if (command.Equals("me"))
        {
            await ReplyAsync(await quoteService.GetQuoteAsync(), isTTS: true);
        }
    }

    // [Command("gpt")]
    // public async Task GptCommand([Remainder] string command)
    // {
    //     var api = new OpenAIClient(Configuration.GetConfiguration<string>("OpenAIKey"));
    //     var messages = new List<Message>
    //     {
    //         new Message(Role.User, "Who won the world series in 2020?"),
    //     };
    //     var chatRequest = new ChatRequest(messages);
    //     var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
    //     Console.WriteLine($"{result.FirstChoice.Message.Role}: {result.FirstChoice.Message.Content}");
    //     //     var response = await gpt.GetResponse(command);
    //     await ReplyAsync("Not implemented yet.");
    // }
}