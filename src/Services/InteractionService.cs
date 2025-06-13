using Discord;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;

namespace radio_discord_bot.Services;

public class InteractionService(IAudioPlayerService audioPlayer, DiscordSocketClient client, GlobalStore globalStore, IQueueService queueService, IConfiguration configuration, ILogger<InteractionService> logger)
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
                var userVoiceChannel = (interaction.User as SocketGuildUser)?.VoiceChannel;
                if (userVoiceChannel is null)
                {
                    await ReplyToChannel.FollowupAsync(component, "You need to be in a voice channel to activate the bot.");
                    return;
                }

                var componentData = _globalStore.Get<SocketMessageComponent>()!.Data;
                var radio = ConfigurationHelper.GetConfiguration<List<Radio>>(configuration, "Radios")
                    .Find(x => x.Name == componentData.CustomId);
                if (componentData.CustomId.Contains("FM"))
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
                    await queueService.AddSongAsync(new Song() { Url = componentData.CustomId, VoiceChannel = userVoiceChannel });
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