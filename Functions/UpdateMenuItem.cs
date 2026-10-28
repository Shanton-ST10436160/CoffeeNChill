using System.Net;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public sealed class UpdateMenuItem
{
    private readonly MenuStorageService _storage;
    public UpdateMenuItem(MenuStorageService storage) => _storage = storage;

    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
        string category, string id)
    {
        UpdateMenuItemRequest? update;
        try { update = await HttpHelpers.ReadJsonAsync<UpdateMenuItemRequest>(req); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid JSON body."); }

        if (update is null)
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "A JSON body is required.");

        try
        {
            await _storage.UpdateAsync(category, id, update);
            return await HttpHelpers.JsonAsync(req, HttpStatusCode.OK, new { message = "Menu item updated.", category, id });
        }
        catch (ArgumentException ex) { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, ex.Message); }
        catch (KeyNotFoundException ex) { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.NotFound, ex.Message); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.InternalServerError, "Could not update menu item."); }
    }
}
