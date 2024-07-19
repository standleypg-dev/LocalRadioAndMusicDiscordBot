using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using radio_discord_bot.Configs;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services.Implementations;

public class QuoteService : IQuoteService
{
    private readonly IHttpRequestService _httpRequestService;
    private readonly Joke_QuoteSetting _quoteConfig;

    public QuoteService(IHttpRequestService httpRequestService)
    {
        _httpRequestService = httpRequestService;
        _quoteConfig = Configuration.GetConfiguration<Joke_QuoteSetting>("QuoteSettings");
    }

    public async Task<string> GetQuoteAsync()
    {
        var quote = await _httpRequestService.GetAsync<Quote>(_quoteConfig.ApiUrl);
        return $"{_quoteConfig.Greeting} {quote.Content} by {quote.Author}";
    }
}
