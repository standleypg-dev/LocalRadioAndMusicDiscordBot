using Discord;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;

namespace radio_discord_bot.Services;

public class InteractionService(
    IAudioPlayerService audioPlayer,
    DiscordSocketClient client,
    GlobalStore globalStore,
    IQueueService queueService,
    IConfiguration configuration,
    ILogger<InteractionService> logger,
    IServiceProvider serviceProvider)
    : IInteractionService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));

    public async Task OnInteractionCreated(SocketInteraction interaction)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            if (interaction is not SocketMessageComponent { Data: { } } component)
            {
                return;
            }

            try
            {
                await component.DeferAsync();
                _globalStore.Set(component);
                var user = interaction.User as SocketGuildUser;

                if (user is not { } guildUser || guildUser.VoiceChannel == null)
                {
                    await ReplyToChannel.FollowupAsync(component, "You need to be in a voice channel to use this command.");
                    return;
                }

                // check if user is self deafened or deafened
                if (guildUser.IsSelfDeafened || guildUser.IsDeafened)
                {
                    await ReplyToChannel.FollowupAsync(component, "You cannot use this command while deafened.");
                    return;
                }

                var componentData = _globalStore.Get<SocketMessageComponent>()!.Data;
                var radio = ConfigurationHelper.GetConfiguration<List<Radio>>(configuration, "Radios")
                    .Find(x => x.Name == componentData.CustomId);
                if (componentData.CustomId.Contains("FM") && componentData.CustomId.Length < 20)
                {
                    if (radio != null)
                    {
                        await ReplyToChannel.FollowupAsync(component, $"Playing {radio.Name} radio station.");
                        await audioPlayer.InitiateVoiceChannelAsync(
                            (interaction.User as SocketGuildUser)?.VoiceChannel, radio.Url);
                    }
                }
                else
                {
                    using var scope = serviceProvider.CreateScope();
                    var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
                    var blacklistService = scope.ServiceProvider.GetRequiredService<IBlacklistService>();

                    // Check if the song is blacklisted, if so, do not add it to the queue
                    if (await blacklistService.IsBlacklistedAsync(componentData.CustomId))
                    {
                        await ReplyToChannel.FollowupAsync(component, "This song is blacklisted and cannot be played.");
                        return;
                    }
                    
                    var song = new Song
                        { Url = componentData.CustomId, VoiceChannel = user.VoiceChannel };

                    await statisticsService.LogSongPlayAsync(component.User, song);

                    await queueService.AddSongAsync(song);
                    await audioPlayer.OnPlaylistChanged();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing interaction: {Message}", e.Message);
            }
        });
    }

    public async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            if (user.IsBot) return; // Skip bot users

            var bot = client.CurrentUser;

            if (bot is null)
            {
                return;
            }

            var botVoiceChannel = audioPlayer.GetBotCurrentVoiceChannel();
            if (botVoiceChannel is null)
            {
                return;
            }

            var isAnyUserStillConnected = botVoiceChannel.ConnectedUsers.Count(u => !u.IsBot) > 0;

            if (!isAnyUserStillConnected)
            {
                await audioPlayer.DestroyVoiceChannelAsync();
            }
        });
    }
}