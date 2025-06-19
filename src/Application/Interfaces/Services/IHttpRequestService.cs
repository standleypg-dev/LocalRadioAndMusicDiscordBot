using Domain.Common.Enums;

namespace Application.Interfaces.Services;

public interface IHttpRequestService
{
    Task<T> GetAsync<T>(string url, object? data = null, string? token = null);
    Task<T> PostAsync<T>(string url, object data, PostRequestMediaType mediaType = PostRequestMediaType.Json);
}
