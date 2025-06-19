namespace Application.Interfaces.Services;

public interface IQuoteService
{
    Task<string> GetQuoteAsync();
}
