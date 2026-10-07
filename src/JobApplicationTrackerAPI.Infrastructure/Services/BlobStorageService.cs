using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace JobApplicationTrackerAPI.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _connectionString;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "documents";

        _blobServiceClient = new BlobServiceClient(_connectionString);
    }

    /// <summary>
    /// Test seam: inject a pre-built client (e.g. a mock) instead of
    /// constructing one from a connection string. DI continues to use the
    /// <see cref="IConfiguration"/> constructor.
    /// </summary>
    public BlobStorageService(BlobServiceClient blobServiceClient, string containerName, string connectionString)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = containerName;
        _connectionString = connectionString;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(fileName);

        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders }, cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task<string> GetSasUrlAsync(string blobUrl, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var blobUri = new Uri(blobUrl);
        var blobName = blobUri.Segments.Last();

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(duration)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var blobClient = new BlobClient(new Uri(blobUrl), new StorageSharedKeyCredential(
            ExtractAccountName(_connectionString),
            ExtractAccountKey(_connectionString)));

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var blobUri = new Uri(blobUrl);
        var blobName = blobUri.Segments.Last();

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static string ExtractAccountName(string connectionString)
    {
        var parts = connectionString.Split(';');
        var accountNamePart = parts.FirstOrDefault(p => p.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase));
        // Substring, not Split('=')[1]: values are base64 and may contain '=' padding.
        return accountNamePart?["AccountName=".Length..] ?? string.Empty;
    }

    private static string ExtractAccountKey(string connectionString)
    {
        var parts = connectionString.Split(';');
        var accountKeyPart = parts.FirstOrDefault(p => p.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase));
        // Substring, not Split('=')[1]: storage keys are base64 and end with '=' padding,
        // which Split would strip and turn into a FormatException in StorageSharedKeyCredential.
        return accountKeyPart?["AccountKey=".Length..] ?? string.Empty;
    }
}
