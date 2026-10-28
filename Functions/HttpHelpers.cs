using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

internal static class HttpHelpers
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    internal static async Task<HttpResponseData> JsonAsync<T>(
        HttpRequestData request, HttpStatusCode status, T value)
    {
        var response = request.CreateResponse(status);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value, JsonOptions));
        return response;
    }

    internal static async Task<HttpResponseData> ErrorAsync(
        HttpRequestData request, HttpStatusCode status, string message)
    {
        return await JsonAsync(request, status, new { error = message });
    }

    internal static async Task<T?> ReadJsonAsync<T>(HttpRequestData request)
    {
        return await JsonSerializer.DeserializeAsync<T>(request.Body, JsonOptions);
    }
}
