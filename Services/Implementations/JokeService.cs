using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services
{
    public class JokeService : IJokeService
    {
        private readonly IHttpRequestService _httpRequestService;
        private readonly Joke_QuoteSetting _jokeConfig;

        public JokeService(IHttpRequestService httpRequestService)
        {
            _httpRequestService = httpRequestService;
            _jokeConfig = Configuration.GetConfiguration<Joke_QuoteSetting>("JokeSettings");
        }

        public async Task<string> GetJokeAsync()
        {
            var joke = await _httpRequestService.GetAsync<Joke>(_jokeConfig.ApiUrl);
            return $"{_jokeConfig.Greeting} {joke.Setup} {joke.Delivery}";
        }
    }
}
