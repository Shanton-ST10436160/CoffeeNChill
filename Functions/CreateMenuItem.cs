using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Net.Http.Headers;

namespace CoffeeNChill.Functions.Functions;

public sealed class CreateMenuItem
{
    private readonly MenuStorageService _storage;
    public CreateMenuItem(MenuStorageService storage) => _storage = storage;

    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
    {
        MenuItem? item;
        try { item = await HttpHelpers.ReadJsonAsync<MenuItem>(req); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid JSON body."); }

        if (item is null)
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "A menu item JSON body is required.");

        try
        {
            await _storage.CreateAsync(item);
            var response = await HttpHelpers.JsonAsync(req, HttpStatusCode.Created, item);
            response.Headers.Add("Location", $"/api/menu/{Uri.EscapeDataString(item.Category)}/{Uri.EscapeDataString(item.Id)}");
            return response;
        }
        catch (ArgumentException ex)
        {
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.Conflict, "An item with this category and ID already exists.");
        }
        catch (RequestFailedException)
        {
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Storage rejected the item. Check that the category and ID are valid.");
        }
    }
}
