using System.Runtime.CompilerServices;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;

namespace radio_discord_bot.Services;

public class InteractionService : IInteractionService
{
    private readonly IAudioService _audioService;
    private readonly DiscordSocketClient _client;

    Dictionary<ulong, HashSet<ulong>> usersInVoiceChannels = new Dictionary<ulong, HashSet<ulong>>();

    public InteractionService(IAudioService audioService, DiscordSocketClient client)
    {
        _audioService = audioService;
        _client = client;
    }

    public async Task OnInteractionCreated(SocketInteraction interaction)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            if (interaction is SocketMessageComponent component)
            {
                if (component.Data is SocketMessageComponentData componentData)
                {
                    await component.DeferAsync();
                    var userVoiceChannel = (interaction.User as SocketGuildUser)?.VoiceChannel;
                    if (userVoiceChannel != null)
                    {
                        var radio = Configuration.GetConfiguration<List<Radio>>("Radios").Find(x => x.Name == componentData.CustomId);
                        if (componentData.CustomId.Contains("FM"))
                        {
                            await FollowupAsync(component, $"Playing {radio.Name} radio station.");
                            await _audioService.InitiateVoiceChannelAsync((interaction.User as SocketGuildUser)?.VoiceChannel, radio.Url);
                        }
                        else
                        {
                            var videoTitle = await _audioService.GetYoutubeTitle(componentData.CustomId);
                            await FollowupAsync(component, $"Added {videoTitle} to queue. Total songs in a queue is {_audioService.GetSongs().Count}");
                            _audioService.AddSong(new Song() { Url = componentData.CustomId, VoiceChannel = userVoiceChannel });
                            await _audioService.OnPlaylistChanged();
                        }
                    }
                    else
                    {
                        await FollowupAsync(component, "You need to be in a voice channel to activate the bot.");
                    }
                }
            }
        });
    }

    public async Task FollowupAsync(SocketMessageComponent component, string msg)
    {
        await component.FollowupAsync(
            text: msg, // Text content of the follow-up message
            isTTS: false,           // Whether the message is text-to-speech
                                    // embeds: new[] { embed }, // Embed(s) to include in the message
            allowedMentions: null,  // Allowed mentions (e.g., roles, users)
            options: null  // Message component options (e.g., buttons)
        );
    }

    public async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        await Task.CompletedTask;
        _ = Task.Run(async () =>
        {
            if (user.IsBot) return; // Skip bot users

            var bot = _client.CurrentUser;

            if (bot != null)
            {
                var botVoiceChannel = _audioService.GetBotCurrentVoiceChannel();
                var guild = botVoiceChannel.Guild;

                // Update the user presence in the dictionary
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
                    await _audioService.DestroyVoiceChannelAsync();
                }
            }
        });
    }
}
