using Application.Configs;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public class QuoteService(IHttpRequestService httpRequestService, IConfiguration configuration) : IQuoteService
{
    private readonly JokeQuoteSettingDto _quoteConfig = ConfigurationHelper.GetConfiguration<JokeQuoteSettingDto>(configuration, "QuoteSettings")!;

    public async Task<string> GetQuoteAsync()
    {
        var quote = await httpRequestService.GetAsync<QuoteDto>(_quoteConfig.ApiUrl);
        return $"{_quoteConfig.Greeting} {quote.Content} by {quote.Author}";
    }
}
