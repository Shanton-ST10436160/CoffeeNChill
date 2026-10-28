using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Net.Http.Headers;

namespace CoffeeNChill.Functions.Functions;

public sealed class GetAllMenuItems
{
    private readonly MenuStorageService _storage;
    public GetAllMenuItems(MenuStorageService storage) => _storage = storage;

    [Function("GetAllMenuItems")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
    {
        try { return await HttpHelpers.JsonAsync(req, HttpStatusCode.OK, await _storage.GetAllAsync()); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.InternalServerError, "Could not retrieve menu items."); }
    }
}
