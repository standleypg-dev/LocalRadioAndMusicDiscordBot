using System.Text.Json;
using AngleSharp.Common;
using Discord;
using Discord.WebSocket;

namespace radio_discord_bot.Services;

public class InteractionsService
{
    private readonly AudioService _audioService;
    public InteractionsService(AudioService audioService)
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

                System.Console.WriteLine($"componentData: {JsonSerializer.Serialize(component.Message.Components?.FirstOrDefault()?.Components?.FirstOrDefault()?.CustomId)}");
                if (component.Data is SocketMessageComponentData componentData)
                {
                    // componentData.CustomId
                    await component.DeferAsync(); // Acknowledge the interaction

                    // var embed = new EmbedBuilder()
                    //     .WithTitle("Lagu dipilih nuan:")
                    //     .WithDescription(componentData.Value)
                    //     .WithColor(Color.Green)
                    //     .Build();


                    var msg = componentData.CustomId.Contains("FM") ? $@"Masang Radio {componentData.CustomId}" : $"Added to playlist. If Radio is playing. {(PlaylistService.playlist.Count > 1 ? "Trigger '\\next' command to start the playlist song." : "")}";


                    if (componentData.CustomId.Contains("FM"))
                    {
                        await component.FollowupAsync(
                                text: msg, // Text content of the follow-up message
                                isTTS: false,           // Whether the message is text-to-speech
                                                        // embeds: new[] { embed }, // Embed(s) to include in the message
                                allowedMentions: null,  // Allowed mentions (e.g., roles, users)
                                options: null  // Message component options (e.g., buttons)
                                );
                        await _audioService.InitiateVoiceChannelAsync((interaction.User as SocketGuildUser)?.VoiceChannel, PlaylistService.RadioList.Find(x => x.Name == componentData.CustomId).Url);
                    }
                    else
                    {
                        PlaylistService.previousPlaylistLength = PlaylistService.playlist.Count;
                        PlaylistService.playlist.Add(new Song
                        {
                            Url = componentData.CustomId,
                            VoiceChannel = (interaction.User as SocketGuildUser)?.VoiceChannel,
                        });
                        await component.FollowupAsync(
                                text: msg, // Text content of the follow-up message
                                isTTS: false,           // Whether the message is text-to-speech
                                                        // embeds: new[] { embed }, // Embed(s) to include in the message
                                allowedMentions: null,  // Allowed mentions (e.g., roles, users)
                                options: null  // Message component options (e.g., buttons)
                                );
                        await _audioService.OnPlaylistChangedAsync(isStartMusic: PlaylistService.previousPlaylistLength == 0 ? true : false);
                    }

                }
            }
        });
    }
}
