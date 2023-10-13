using System.Text.Json;
using AngleSharp.Common;
using Discord;
using Discord.WebSocket;

namespace radio_discord_bot.Services;

public class InteractionsService
{
    private readonly AudioService _audioService;
    public InteractionsService(AudioService audioService, PlaylistService playlistService)
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

                    if (componentData.CustomId.Contains("FM"))
                        await _audioService.InitiateVoiceChannelAsync((interaction.User as SocketGuildUser)?.VoiceChannel, Constants.radios.Find(x => x.Title == componentData.CustomId).Url);
                    else
                    {
                        PlaylistService.playlist.Add(new Song
                        {
                            url = componentData.CustomId,
                            voiceChannel = (interaction.User as SocketGuildUser)?.VoiceChannel,
                        });
                        await _audioService.OnPlaylistChangedAsync();
                    }

                    await component.FollowupAsync(
                            text: componentData.CustomId.Contains("FM") ? $"Masang Radio {componentData.CustomId}" : "Masang lagu..", // Text content of the follow-up message
                            isTTS: false,           // Whether the message is text-to-speech
                                                    // embeds: new[] { embed }, // Embed(s) to include in the message
                            allowedMentions: null,  // Allowed mentions (e.g., roles, users)
                            options: null  // Message component options (e.g., buttons)
                            );
                }
            }
        });
    }
}
