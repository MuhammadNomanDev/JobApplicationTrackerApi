using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using JobApplicationTracker.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace JobApplicationTracker.Infrastructure.Services;

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
        return accountNamePart?.Split('=')[1] ?? string.Empty;
    }

    private static string ExtractAccountKey(string connectionString)
    {
        var parts = connectionString.Split(';');
        var accountKeyPart = parts.FirstOrDefault(p => p.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase));
        return accountKeyPart?.Split('=')[1] ?? string.Empty;
    }
}
