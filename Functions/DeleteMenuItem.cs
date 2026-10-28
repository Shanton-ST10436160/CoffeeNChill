using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Net.Http.Headers;

namespace CoffeeNChill.Functions.Functions;

public sealed class DeleteMenuItem
{
    private readonly MenuStorageService _storage;
    public DeleteMenuItem(MenuStorageService storage) => _storage = storage;

    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
        string category, string id)
    {
        try
        {
            await _storage.DeleteAsync(category, id);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (KeyNotFoundException ex) { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.NotFound, ex.Message); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.InternalServerError, "Could not delete menu item."); }
    }
}
