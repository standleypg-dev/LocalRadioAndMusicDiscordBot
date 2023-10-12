using System.Net.WebSockets;
using System.Reflection.Metadata;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using radio_discord_bot;
using radio_discord_bot.Handlers;
using YoutubeExplode;
using YoutubeExplode.Common;

public class Program
{
    private const string Token = "MTA0MjY4NzQ4OTMyNTk0MDc4Ng.GnELXy.LFH092CTYy9jVgebs9KTPKckmi6pzx7ABXEdT4";
    private readonly DiscordSocketClient _client;
    private readonly AudioService _audioService;
    private readonly YoutubeClient _youtubeClient;

    public static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    public Program()
    {
        _youtubeClient = new YoutubeClient();

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.DirectMessages
        };
        _client = new DiscordSocketClient(config);

        _audioService = new AudioService();
    }

    public async Task RunBotAsync()
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += TextChannelMessageReceivedAsync;

        await _client.LoginAsync(TokenType.Bot, Token);
        await _client.StartAsync();

        _client.InteractionCreated += OnInteractionCreated;

        await Task.Delay(-1);
    }

    public async Task TextChannelMessageReceivedAsync(SocketMessage arg)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            var message = arg as SocketUserMessage;
            if (message.Author.IsBot) return;
            var channel = message.Channel as SocketTextChannel;
            System.Console.WriteLine(message.Author as IGuildUser);
            if (message.ToString().StartsWith("/"))
            {
                var voiceChannel = (message.Author as IGuildUser)?.VoiceChannel;

                if (voiceChannel == null)
                {
                    await channel.SendMessageAsync("Nuan mesti ba dalam voice channel enti ka masang ngena command tu.");
                    return;
                }

                var command = message.ToString().Substring(1).Trim();
                System.Console.WriteLine($"command: {command}");

                await CommandHandler.HandleCommand(command, _youtubeClient, _audioService, channel, message, voiceChannel);
            }
        });
    }


    private async Task OnInteractionCreated(SocketInteraction interaction)
    {
        _ = Task.Run(async () =>
        {
            if (interaction is SocketMessageComponent component)
            {
                if (component.Data is SocketMessageComponentData buttonData)
                {

                    // buttonData.CustomId
                    await component.DeferAsync(); // Acknowledge the interaction

                    // var embed = new EmbedBuilder()
                    //     .WithTitle("Lagu dipilih nuan:")
                    //     .WithDescription(buttonData.Value)
                    //     .WithColor(Color.Green)
                    //     .Build();

                    if (buttonData.CustomId.Contains("FM"))
                        await _audioService.InitiateVoiceChannelAsync((interaction.User as SocketGuildUser)?.VoiceChannel, Constants.radios.Find(x => x.Title == buttonData.CustomId).Url);
                    else
                        await _audioService.InitiateVoiceChannelAsyncYt((interaction.User as SocketGuildUser)?.VoiceChannel, buttonData.CustomId);

                    await component.FollowupAsync(
                            text: buttonData.CustomId.Contains("FM") ? $"Masang Radio {buttonData.CustomId}" : "Masang lagu..", // Text content of the follow-up message
                            isTTS: false,           // Whether the message is text-to-speech
                                                    // embeds: new[] { embed }, // Embed(s) to include in the message
                            allowedMentions: null,  // Allowed mentions (e.g., roles, users)
                            options: null  // Message component options (e.g., buttons)
                            );
                }
            }
        });
    }

    public async Task ReadyAsync()
    {
        await Task.CompletedTask;
        Console.WriteLine($"Logged in as {_client.CurrentUser.Username}");
    }

    private async Task LogAsync(LogMessage log)
    {
        await Task.CompletedTask;
        Console.WriteLine(log);
    }
}
