using Discord;
using YoutubeExplode.Search;

namespace radio_discord_bot.Helpers;

public static class MessageComponentGenerator
{
    public static MessageComponent GenerateComponents<T>(List<T> elem)
    {
        var rows = new List<ActionRowBuilder>();
        int idx = 0;
        foreach (var item in elem)
        {
            idx++;
            var button = new ButtonBuilder()
                .WithStyle(ButtonStyle.Primary);

            if (item is Radio radio)
            {
                button.WithLabel($"{idx}. {radio.Title}")
                      .WithCustomId($"{radio.Title}");
            }
            else if (item is VideoSearchResult playlist)
            {
                System.Console.WriteLine($"playlist: {playlist.Title} {playlist.Url}");
                button.WithLabel($"{idx}. {playlist.Title}")
                      .WithCustomId($"{playlist.Url}");
            }
            // add more else if blocks for other types of items as needed

            rows.Add(new ActionRowBuilder().AddComponent(button.Build()));
        }
        return new ComponentBuilder().WithRows(rows: rows).Build();
    }
}
