using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace CoffeeNChill.Gateway.Services;

public sealed class BackendApiService
{
    private readonly HttpClient _http;
    private readonly Uri _baseAddress;

    public BackendApiService(HttpClient http, IConfiguration configuration)
    {
        var baseUrl = configuration["Backend:FunctionsBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed))
            throw new InvalidOperationException("Backend:FunctionsBaseUrl must be an absolute URL.");
        _baseAddress = new Uri(parsed.ToString().TrimEnd('/') + "/");
        _http = http;
    }

    public Task ForwardIncomingAsync(HttpContext context, string relativePath, CancellationToken ct = default) =>
        ForwardCoreAsync(context, relativePath, null, true, ct);

    public Task ForwardJsonAsync(HttpContext context, string relativePath, object body, CancellationToken ct = default) =>
        ForwardCoreAsync(context, relativePath, JsonContent.Create(body), false, ct);

    public Task ForwardAsync(HttpContext context, string relativePath, CancellationToken ct = default) =>
        ForwardCoreAsync(context, relativePath, null, false, ct);

    private async Task ForwardCoreAsync(HttpContext context, string relativePath, HttpContent? suppliedContent, bool useIncomingBody, CancellationToken ct)
    {
        var pathAndQuery = relativePath.TrimStart('/') + context.Request.QueryString.ToUriComponent();
        using var outbound = new HttpRequestMessage(new HttpMethod(context.Request.Method), new Uri(_baseAddress, pathAndQuery));
        if (suppliedContent is not null)
            outbound.Content = suppliedContent;
        else if (useIncomingBody && context.Request.Method is not "GET" and not "HEAD")
        {
            outbound.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                outbound.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
            if (context.Request.ContentLength.HasValue)
                outbound.Content.Headers.ContentLength = context.Request.ContentLength.Value;
        }

        if (context.Request.Headers.TryGetValue("Accept", out var accept))
            outbound.Headers.TryAddWithoutValidation("Accept", accept.ToArray());

        using var response = await _http.SendAsync(outbound, HttpCompletionOption.ResponseHeadersRead, ct);
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
            if (!IsHopByHop(header.Key)) context.Response.Headers[header.Key] = header.Value.ToArray();
        foreach (var header in response.Content.Headers)
            if (!IsHopByHop(header.Key)) context.Response.Headers[header.Key] = header.Value.ToArray();
        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, ct);
    }

    private static bool IsHopByHop(string name) => name.Equals("Connection", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
        || name.Equals("TE", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Trailer", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);
}
