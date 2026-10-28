using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public sealed class UploadStaffDocument
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private readonly DocumentStorageService _storage;
    public UploadStaffDocument(DocumentStorageService storage) => _storage = storage;

    [Function("UploadStaffDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
    {
        if (!MediaTypeHeaderValue.TryParse(req.Headers.GetValues("Content-Type").FirstOrDefault(), out var mediaType) ||
            !string.Equals(mediaType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Send a multipart/form-data request with a file field.");

        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Multipart boundary is missing.");

        try
        {
            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync()) is not null)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
                    continue;

                var fieldName = HeaderUtilities.RemoveQuotes(disposition.Name).Value;
                var fileName = HeaderUtilities.RemoveQuotes(disposition.FileNameStar.HasValue
                    ? disposition.FileNameStar : disposition.FileName).Value;

                if (!string.Equals(fieldName, "file", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(fileName))
                    continue;

                fileName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 200)
                    return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Invalid filename.");

                var contentType = section.ContentType ?? "application/octet-stream";
                var total = await _storage.UploadAsync(fileName, section.Body, contentType, MaxUploadBytes);
                return await HttpHelpers.JsonAsync(req, HttpStatusCode.Created, new
                {
                    message = "Document uploaded.",
                    fileName,
                    size = total
                });
            }

            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "No file found. Use the form-data key 'file'.");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 409)
        {
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.Conflict, "A document with that filename already exists.");
        }
        catch (InvalidDataException ex)
        {
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.RequestEntityTooLarge, ex.Message);
        }
        catch (Exception)
        {
            return await HttpHelpers.ErrorAsync(req, HttpStatusCode.BadRequest, "Could not process the multipart upload. Check the file and storage connection.");
        }
    }
}
