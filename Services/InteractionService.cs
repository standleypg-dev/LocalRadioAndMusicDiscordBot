using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;

namespace radio_discord_bot.Services;

public class InteractionService(IAudioPlayerService audioPlayer, DiscordSocketClient client, GlobalStore globalStore, IQueueService queueService)
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

            await component.DeferAsync();
            _globalStore.Set(component);
            var userVoiceChannel = (interaction.User as SocketGuildUser)?.VoiceChannel;
            if (userVoiceChannel is null)
            {
                await ReplyToChannel.FollowupAsync(component, "You need to be in a voice channel to activate the bot.");
                return;
            }

            var componentData = _globalStore.Get<SocketMessageComponent>()!.Data;
            var radio = Configuration.GetConfiguration<List<Radio>>("Radios")
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

            var guild = botVoiceChannel.Guild;

            // Update the user presence in the dictionary
            _globalStore.Set(new Dictionary<ulong, HashSet<ulong>>());
            var usersInVoiceChannels = _globalStore.Get<Dictionary<ulong, HashSet<ulong>>>() ??
                                       new Dictionary<ulong, HashSet<ulong>>();

            if (!usersInVoiceChannels.ContainsKey(guild.Id))
            {
                usersInVoiceChannels[guild.Id] = new HashSet<ulong>();
            }

            if (oldState.VoiceChannel != null)
            {
                usersInVoiceChannels[guild.Id].Remove(user.Id);
            }

            if (newState.VoiceChannel != null && newState.VoiceChannel.Id == botVoiceChannel.Id)
            {
                usersInVoiceChannels[guild.Id].Add(user.Id);
            }

            var membersInBotChannel = usersInVoiceChannels[guild.Id];

            if (membersInBotChannel.Count == 0)
            {
                await audioPlayer.DestroyVoiceChannelAsync();
            }
        });
    }
}