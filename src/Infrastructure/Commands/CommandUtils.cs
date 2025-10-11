using Domain.Common;
using Domain.Eventing;
using Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Infrastructure.Commands;

public static class CommandUtils
{
    internal static T CreateMessage<T>(string message) where T : IMessageProperties, new()
    {
        return new()
        {
            Content = message,
            Components = [],
        };
    }

    internal static IEnumerable<IMessageComponentProperties> CreateStringMenuComponent<T>(T source)
        where T : IEnumerable<ComponentModel>
    {
        return
        [
            new StringMenuProperties(Constants.CustomIds.Play)
            {
                Options = source.Select(s => new StringMenuSelectOptionProperties(s.Title, s.Url)
                {
                    Description = s.Description ?? string.Empty,
                }).ToList()
            }
        ];
    }

    internal static IEnumerable<IMessageComponentProperties> CreateButtonComponent<T>(T source)
        where T : IEnumerable<ComponentModel>
    {
        return
        [
            new ActionRowProperties
            {
                Components = source.Select(s => new ButtonProperties($"{Constants.CustomIds.Play}:{s.Url}", s.Title, ButtonStyle.Primary)).ToList()
            }
        ];
    }
    
    internal static async Task<bool> NotInVoiceChannel(ApplicationCommandContext context, Func<InteractionCallbackProperties<InteractionMessageProperties>, Task> respondAsync)
    {
        if (!context.Guild!.VoiceStates.TryGetValue(context.User.Id, out _))
        {
            var notInVoiceChannelMessage =
                CreateMessage<InteractionMessageProperties>("You must be in a voice channel to use this command.");
            await respondAsync(InteractionCallback.Message(notInVoiceChannelMessage));
            return true;
        }

        return false;
    }

    internal record ComponentModel(string Title, string Url, string? Description = null);
    
    
}