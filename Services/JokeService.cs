using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace radio_discord_bot.Services
{
    public class JokeService : IJokeService
    {
        private readonly DiscordSocketClient _client;
        private bool isJokeEnabled = false;

        public JokeService(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task Start()
        {
            await Task.CompletedTask;
            var timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public async void OnTimerElapsed(object state)
        {
            // TODO: Implement joke retrieval and sending logic

            if (isJokeEnabled)
            {
                var textChannel = await FindTextChannelByNameAsync(_client, "tts-wai");
                System.Console.WriteLine(textChannel);

                if (textChannel != null)
                    await textChannel.SendMessageAsync("Hello world!", isTTS: true);
            }
        }

        private static async Task<ITextChannel> FindTextChannelByNameAsync(IDiscordClient client, string channelName)
        {
            foreach (var guild in (await client.GetGuildsAsync()))
            {
                var textChannels = await guild.GetTextChannelsAsync();

                var textChannel = textChannels.FirstOrDefault(ch => ch.Name == channelName);
                return textChannel;
            }
            return null;
        }

        public async Task EnableJoke()
        {
            isJokeEnabled = true;
            await Task.CompletedTask;
        }

        public async Task DisableJoke()
        {
            isJokeEnabled = false;
            await Task.CompletedTask;
        }
    }
}
