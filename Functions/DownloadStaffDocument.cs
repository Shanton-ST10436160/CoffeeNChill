using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Net.Http.Headers;

namespace CoffeeNChill.Functions.Functions;

public sealed class DownloadStaffDocument
{
    private readonly DocumentStorageService _storage;
    public DownloadStaffDocument(DocumentStorageService storage) => _storage = storage;

    [Function("DownloadStaffDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid filename.");

        try
        {
            if (!await _storage.ExistsAsync(fileName))
                return await HttpHelpers.ErrorAsync(req, HttpStatusCode.NotFound, "Document not found.");

            var result = await _storage.DownloadAsync(fileName);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", result.Details.ContentType ?? "application/octet-stream");
            response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName.Replace("\"", "")}\"");
            await result.Content.CopyToAsync(response.Body);
            return response;
        }
        catch { return await HttpHelpers.ErrorAsync(req, HttpStatusCode.InternalServerError, "Could not download document."); }
    }
}
