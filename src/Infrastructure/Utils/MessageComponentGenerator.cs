using Application.DTOs;
using ApplicationDto.DTOs;
using ApplicationDto.DTOs.Spotify;
using Discord;
using YoutubeExplode.Search;

namespace Infrastructure.Utils;

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

            if (item is RadioDto radio)
            {
                button.WithLabel($"{idx}. {radio.Name}")
                      .WithCustomId($"{radio.Name}");
            }
            else if (item is VideoSearchResult ytVideo)
            {
                button.WithLabel($"{idx}. {(ytVideo.Title.Length > 70 ? ytVideo.Title.Substring(0, 70) : ytVideo.Title)}")
                      .WithCustomId($"{ytVideo.Url}");
            }
            else if(item is Items baseSearch)
            {
                button.WithLabel($"{idx}. {baseSearch.Name} - {baseSearch.Artists.FirstOrDefault()?.Name}")
                      .WithCustomId($"{baseSearch.Id}");
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
