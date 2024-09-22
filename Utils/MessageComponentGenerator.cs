using Discord;
using radio_discord_bot.Models;
using YoutubeExplode.Search;

namespace radio_discord_bot.Utils;

public static class MessageComponentGenerator
{
    public static MessageComponent GenerateComponents<T>(List<T> elem, int colInRow = 1)
    {
        var rows = new List<ActionRowBuilder>();
        int idx = 0;
        var currentRow = new ActionRowBuilder();
        foreach (var item in elem)
        {
            idx++;
            var button = new ButtonBuilder()
                .WithStyle(ButtonStyle.Primary);

            if (item is Radio radio)
            {
                button.WithLabel($"{idx}. {radio.Name}")
                      .WithCustomId($"{radio.Name}");
            }
            else if (item is VideoSearchResult ytVideo)
            {
                button.WithLabel($"{idx}. {(ytVideo.Title.Length > 70 ? ytVideo.Title.Substring(0, 70) : ytVideo.Title)}")
                      .WithCustomId($"{ytVideo.Url}");
            }

            currentRow.AddComponent(button.Build());

            if (idx % colInRow == 0)
            {
                rows.Add(currentRow);
                currentRow = new ActionRowBuilder();
            }
        }

        if (currentRow.Components.Count > 0)
        {
            rows.Add(currentRow);
        }

        return new ComponentBuilder().WithRows(rows: rows).Build();
    }
}
