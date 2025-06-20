using Application.Configs;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

    public class JokeService(IHttpRequestService httpRequestService, IConfiguration configuration)
        : IJokeService
    {
        private readonly JokeQuoteSettingDto _jokeConfig = configuration.GetConfiguration<JokeQuoteSettingDto>("JokeSettings")!;

        public async Task<string> GetJokeAsync()
        {
            var joke = await httpRequestService.GetAsync<JokeDto>(_jokeConfig.ApiUrl);
            return $"{_jokeConfig.Greeting} {joke.Setup} {joke.Delivery}";
        }
    }

