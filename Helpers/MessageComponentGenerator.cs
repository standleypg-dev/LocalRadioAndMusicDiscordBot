using System.Text.Json;
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
            // System.Console.WriteLine($"item: {item}");
            idx++;
            var button = new ButtonBuilder()
                .WithStyle(ButtonStyle.Primary);

            if (item is Radio radio)
            {
                button.WithLabel($"{idx}. {radio.Title}")
                      .WithCustomId($"{radio.Title}");
            }
            else if (item is VideoSearchResult ytVideo)
            {
                button.WithLabel($"{idx}. {(ytVideo.Title.Length > 70 ? ytVideo.Title.Substring(0, 70)  : ytVideo.Title)}")
                      .WithCustomId($"{ytVideo.Url}");
            }
            else if(item is Song playlist){
                button.WithLabel($"{idx}. {(playlist.Url.Length > 70 ? playlist.Url.Substring(0, 70)  : playlist.Url)}")
                      .WithCustomId($"{playlist.Url}");
            }
            // add more else if blocks for other types of items as needed

            rows.Add(new ActionRowBuilder().AddComponent(button.Build()));
        }
        return new ComponentBuilder().WithRows(rows: rows).Build();
    }
}
