using System.Runtime.CompilerServices;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;

namespace radio_discord_bot.Services;

public class InteractionService : IInteractionService
{
    private readonly IAudioService _audioService;

    public InteractionService(IAudioService audioService)
    {
        _audioService = audioService;
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
                    if (componentData.CustomId.Contains("FM"))
                    {
                        await FollowupAsync(component, "Playing radio..");
                        await _audioService.InitiateVoiceChannelAsync((interaction.User as SocketGuildUser)?.VoiceChannel, Configuration.GetConfiguration<List<Radio>>("Radios").Find(x => x.Name == componentData.CustomId).Url);
                    }
                    else
                    {
                        await FollowupAsync(component, _audioService.GetSongs().Count() > 0 ? $"Added to queue. Total songs in a queue is {_audioService.GetSongs().Count()}" : "Playing song..");
                        _audioService.AddSong(new Song() { Url = componentData.CustomId, VoiceChannel = (interaction.User as SocketGuildUser)?.VoiceChannel });
                        await _audioService.OnPlaylistChanged();
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
}
