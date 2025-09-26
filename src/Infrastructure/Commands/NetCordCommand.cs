using Domain.Common;
using Domain.Eventing;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.Commands;
using SoundCloudExplode;
using SoundCloudExplode.Common;
using Constants = Domain.Common.Constants;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Infrastructure.Commands;

public class NetCordCommand(IServiceProvider serviceProvider, IConfiguration configuration, SoundCloudClient soundCloudClient): CommandModule<CommandContext>
{
    [RequireUserPermissions<CommandContext>(Permissions.Administrator)]
    [Command("play")]
    public async Task PingAsync([CommandParameter(Remainder = true)] string command)
    {
        await soundCloudClient.InitializeAsync();
        var source =
            await soundCloudClient.Search.GetTracksAsync(command)
                .CollectAsync(6); 

        var message = CreateMessage<MessageProperties>();
        message.Components =
        [
            new StringMenuProperties(Constants.CustomIds.Play)
            {
                Options = source.Select(s => new StringMenuSelectOptionProperties(s.Title!, s.Url!)
                {
                    Description = s.User?.FullName
                }).ToList()
            }
        ];
        await SendAsync(message);
    }
    
    [Command("stop")]
    public async Task Stop()
    {
        using var scope = serviceProvider.CreateScope();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        eventDispatcher.Dispatch(new EventType.Stop());
        await SendAsync(CreateMessage<MessageProperties>());
    }
    
    [Command("next", "skip")]
    public async Task Skip()
    {
        using var scope = serviceProvider.CreateScope();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
        eventDispatcher.Dispatch(new EventType.Skip());
        await SendAsync(CreateMessage<MessageProperties>());
    }
    
    [Command("username")]
    public string Username(User? user = null)
    {
        user ??= Context.User;
        return user.Username;
    }
    
    static T CreateMessage<T>() where T : IMessageProperties, new()
    {
        return new()
        {
            Content = "Hello, World!",
            Components = [],
        };
    }
}