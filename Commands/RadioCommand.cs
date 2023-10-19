using Discord;
using Discord.Commands;
using Discord.Interactions;
using OpenAI;
using OpenAI.Chat;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Utils;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace radio_discord_bot.Commands;

public class RadioCommand : ModuleBase<SocketCommandContext>
{
    private readonly YoutubeClient _youtubeClient;
    private readonly IAudioService _audioService;
    private readonly IJokeService _jokeService;
    private readonly IQuoteService _quoteService;

    public RadioCommand(IAudioService audioService, YoutubeClient youtubeClient, IJokeService jokeService, IQuoteService quoteService)
    {
        _audioService = audioService;
        _youtubeClient = youtubeClient;
        _jokeService = jokeService;
        _quoteService = quoteService;
    }

    [Command("play")]
    public async Task HelloCommand([Remainder] string command)
    {
        if (command.Equals("radio"))
        {
            MessageComponentGenerator.GenerateComponents(Configuration.GetConfiguration<List<Radio>>("Radios"), colInRow: 2);
            var embed = new EmbedBuilder()
                .WithTitle("Choose your favorite radio station:")
                .WithFooter("Powered by RMT & Astro")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(Configuration.GetConfiguration<List<Radio>>("Radios"), colInRow: 3));
        }
        else if (Uri.TryCreate(command, UriKind.Absolute, out _))
        {
            var videos = await _youtubeClient.Search.GetVideosAsync(command).CollectAsync(5); // Adjust the number of results we want

            var embed = new EmbedBuilder()
                .WithTitle("Click to play or to add to the queue:")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(videos.ToList()));
        }
        else
        {
            var videos = await _youtubeClient.Search.GetVideosAsync(command).CollectAsync(5); // Adjust the number of results we want

            var embed = new EmbedBuilder()
                .WithTitle("Choose your song")
                .Build();

            await ReplyAsync(embed: embed, components: MessageComponentGenerator.GenerateComponents(videos.ToList()));
        }

    }

    [Command("help")]
    public async Task HelpCommand()
    {
        var helpMessage = Configuration.GetConfiguration<HelpMessage>("HelpMessage");
        var embed = new EmbedBuilder()
            .WithTitle(helpMessage.Title)
            .WithDescription(helpMessage.Description)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("stop")]
    public async Task StopCommand()
    {
        await ReplyAsync("Stopping radio..");
        await _audioService.DestroyVoiceChannelAsync();
        await _audioService.EmptyPlaylist();

    }

    [Command("next")]
    public async Task NextCommand()
    {
        if (_audioService.GetSongs().Count == 1)
            await ReplyAsync("No songs in queue. Nothing to next.");
        else
        {
            await ReplyAsync("Playing next song..");
            await _audioService.NextSongAsync();
        }
    }

    [Command("playlist")]
    public async Task QueueCommand()
    {
        var songs = _audioService.GetSongs();

        await ReplyAsync("Queues: \n" + string.Join("\n", songs.Select((song, index) => $"{index + 1}. {song.Url}")));
    }

    [Command("tell")]
    public async Task TellJoke([Remainder] string command)
    {
        if (command.Equals("joke"))
        {
            await ReplyAsync(await _jokeService.GetJokeAsync(), isTTS: true);
        }
    }

    [Command("motivate")]
    public async Task TellQuote([Remainder] string command)
    {
        if (command.Equals("me"))
        {
            await ReplyAsync(await _quoteService.GetQuoteAsync(), isTTS: true);
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

