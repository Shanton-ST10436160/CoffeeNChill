using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Net.Http.Headers;

namespace CoffeeNChill.Functions.Functions;

public sealed class GetMenuItemsByCategory
{
    private readonly MenuStorageService _storage;
    public GetMenuItemsByCategory(MenuStorageService storage) => _storage = storage;

    [Function("GetMenuItemsByCategory")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
        string category)
    {
        try { return await HttpHelpers.JsonAsync(req, HttpStatusCode.OK, await _storage.GetByCategoryAsync(category)); }
        catch (ArgumentException ex) { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, ex.Message); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.InternalServerError, "Could not filter menu items."); }
    }
}
