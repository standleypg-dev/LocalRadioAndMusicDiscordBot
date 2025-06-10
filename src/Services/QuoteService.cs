using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services;

public class QuoteService(IHttpRequestService httpRequestService, IConfiguration configuration) : IQuoteService
{
    private readonly JokeQuoteSetting _quoteConfig = ConfigurationHelper.GetConfiguration<JokeQuoteSetting>(configuration, "QuoteSettings")!;

    public async Task<string> GetQuoteAsync()
    {
        var quote = await httpRequestService.GetAsync<Quote>(_quoteConfig.ApiUrl);
        return $"{_quoteConfig.Greeting} {quote.Content} by {quote.Author}";
    }
}
