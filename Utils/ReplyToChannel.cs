using Discord.WebSocket;

namespace radio_discord_bot.Utils;

public static class ReplyToChannel
{
    /// <summary>
    /// Followup to the channel with the message.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="msg"></param>
    public static async Task FollowupAsync(SocketMessageComponent component, string msg)
    {
        await component.FollowupAsync(
            text: msg, // Text content of the follow-up message
            isTTS: false, // Whether the message is text-to-speech
            // embeds: new[] { embed }, // Embed(s) to include in the message
            allowedMentions: null, // Allowed mentions (e.g., roles, users)
            options: null // Message component options (e.g., buttons)
        );
    }
}