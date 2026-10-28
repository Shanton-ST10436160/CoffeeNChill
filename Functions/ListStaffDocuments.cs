using System.Net;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public sealed class ListStaffDocuments
{
    private readonly DocumentStorageService _storage;
    public ListStaffDocuments(DocumentStorageService storage) => _storage = storage;

    [Function("ListStaffDocuments")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
    {
        try { return await HttpHelpers.JsonAsync(req, HttpStatusCode.OK, await _storage.ListAsync()); }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.InternalServerError, "Could not list staff documents."); }
    }
}
