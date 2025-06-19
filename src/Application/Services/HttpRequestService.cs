using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services;
using Domain.Common.Enums;

namespace Application.Services;

public class HttpRequestService : IHttpRequestService
{
    public async Task<T> GetAsync<T>(string url, object? data = null, string? token = null)
    {
        using var httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (data is not null)
        {
            var query = string.Join("&", data.GetType().GetProperties().Select(x => $"{x.Name}={x.GetValue(data)}"));
            url += $"?{query}";
        }

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content) ??
               throw new HttpRequestException(
                   $"Error deserializing data from the server. Status code: {response.StatusCode}");
    }

    public async Task<T> PostAsync<T>(string url, object data,
        PostRequestMediaType mediaType = PostRequestMediaType.Json)
    {
        using var httpClient = new HttpClient();
        HttpResponseMessage? response = null;
        if (mediaType == PostRequestMediaType.Json)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            response = await httpClient.PostAsync(url, content);
        }
        else if (mediaType == PostRequestMediaType.FormUrlEncoded)
        {
            var content = new FormUrlEncodedContent(ObjectToKeyValuePairs(data));
            response = await httpClient.PostAsync(url, content);
        }

        response?.EnsureSuccessStatusCode();
        var responseContent = await response!.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent) ??
               throw new HttpRequestException(
                   $"Error deserializing data from the server. Status code: {response.StatusCode}");
    }

    private static Dictionary<string, string> ObjectToKeyValuePairs(object obj)
    {
        var keyValuePairs = new Dictionary<string, string>();
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            var value = property.GetValue(obj)?.ToString();
            if (value != null)
            {
                keyValuePairs.Add(property.Name, value);
            }
        }

        return keyValuePairs;
    }
}