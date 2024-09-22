namespace radio_discord_bot.Services.Interfaces;

public interface IQuoteService
{
    Task<string> GetQuoteAsync();
}
