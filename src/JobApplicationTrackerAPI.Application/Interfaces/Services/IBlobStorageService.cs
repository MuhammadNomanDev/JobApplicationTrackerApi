namespace JobApplicationTracker.Application.Interfaces.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetSasUrlAsync(string blobUrl, TimeSpan duration, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
