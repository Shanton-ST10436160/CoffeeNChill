using Microsoft.Extensions.Configuration;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CoffeeNChill.Functions.Services;

public sealed class DocumentStorageService
{
    private readonly BlobContainerClient _container;

    public DocumentStorageService(IConfiguration configuration)
    {
        var connection = configuration["StorageConnection"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("StorageConnection is not configured.");

        _container = new BlobServiceClient(connection).GetBlobContainerClient("staff-docs");
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<long> UploadAsync(string fileName, Stream content, string contentType, long maxBytes)
    {
        var blob = _container.GetBlobClient(fileName);
        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType
        };
        long total = 0;
        try
        {
            await using var destination = await blob.OpenWriteAsync(
                overwrite: false,
                options: new BlobOpenWriteOptions { HttpHeaders = headers });
            var buffer = new byte[81920];
            int read;
            while ((read = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                    throw new InvalidDataException($"Maximum upload size is {maxBytes} bytes.");
                await destination.WriteAsync(buffer, 0, read);
            }
            await destination.FlushAsync();
            return total;
        }
        catch
        {
            try { await blob.DeleteIfExistsAsync(); } catch { /* Preserve original exception. */ }
            throw;
        }
    }

    public async Task<List<object>> ListAsync()
    {
        var files = new List<object>();
        await foreach (var item in _container.GetBlobsAsync())
        {
            files.Add(new
            {
                FileName = item.Name,
                Size = item.Properties.ContentLength,
                LastModified = item.Properties.LastModified,
                ContentType = item.Properties.ContentType
            });
        }
        return files;
    }

    public async Task<BlobDownloadStreamingResult> DownloadAsync(string fileName)
    {
        return await _container.GetBlobClient(fileName).DownloadStreamingAsync();
    }

    public async Task<bool> ExistsAsync(string fileName)
    {
        return (await _container.GetBlobClient(fileName).ExistsAsync()).Value;
    }
}
