using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services.Implementations;

    public class JokeService : IJokeService
    {
        private readonly IHttpRequestService _httpRequestService;
        private readonly JokeQuoteSetting _jokeConfig;

        public JokeService(IHttpRequestService httpRequestService)
        {
            _httpRequestService = httpRequestService;
            _jokeConfig = Configuration.GetConfiguration<JokeQuoteSetting>("JokeSettings");
        }

        public async Task<string> GetJokeAsync()
        {
            var joke = await _httpRequestService.GetAsync<Joke>(_jokeConfig.ApiUrl);
            return $"{_jokeConfig.Greeting} {joke.Setup} {joke.Delivery}";
        }
    }

