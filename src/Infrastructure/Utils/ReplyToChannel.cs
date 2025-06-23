using Discord;
using Discord.WebSocket;

namespace Infrastructure.Utils;

public static class ReplyToChannel
{
    public static async Task FollowupAsync(this SocketMessageComponent component, string msg)
    {
        await component.FollowupAsync(
            text: msg
        );
    }
    public static async Task FollowupEmbebAsync(this SocketMessageComponent socketMessageComponent,
        MessageComponent? component = null, Embed? embed = null)
    {
        await socketMessageComponent.FollowupAsync(
            embeds: [embed],
            components: component
        );
    }
}