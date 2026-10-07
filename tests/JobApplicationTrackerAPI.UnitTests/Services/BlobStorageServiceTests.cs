using System.Text;
using AwesomeAssertions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JobApplicationTrackerAPI.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace JobApplicationTrackerAPI.UnitTests.Services;

/// <summary>
/// P1/M1i: blob storage tests. Upload/Delete use hand-written fakes
/// (Moq can't disambiguate the Azure SDK's optional-parameter overloads in
/// expression trees); GetSasUrlAsync signs locally with no network, so it runs
/// against the real service with a fake connection string.
/// </summary>
public class BlobStorageServiceTests
{
    private const string ContainerName = "documents";
    private const string BlobUrl = "https://testaccount.blob.core.windows.net/documents/cv.pdf";

    private static string FakeConnectionString { get; } =
        "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=" +
        Convert.ToBase64String(Encoding.UTF8.GetBytes("01234567890123456789012345678901")) +
        ";EndpointSuffix=core.windows.net";

    private sealed class FakeBlobClient : BlobClient
    {
        public int DeleteIfExistsCalls { get; private set; }
        public BlobUploadOptions? LastUploadOptions { get; private set; }
        public Stream? LastUploadStream { get; private set; }

        public override Uri Uri => new(BlobUrl);

        public override Task<Response<BlobContentInfo>> UploadAsync(
            Stream content, BlobUploadOptions options, CancellationToken cancellationToken = default)
        {
            LastUploadStream = content;
            LastUploadOptions = options;
            return Task.FromResult<Response<BlobContentInfo>>(null!);
        }

        public override Task<Response<bool>> DeleteIfExistsAsync(
            DeleteSnapshotsOption snapshotsOption = default,
            BlobRequestConditions conditions = default!,
            CancellationToken cancellationToken = default)
        {
            DeleteIfExistsCalls++;
            return Task.FromResult<Response<bool>>(null!);
        }
    }

    private sealed class FakeBlobContainerClient : BlobContainerClient
    {
        private readonly FakeBlobClient _blob;
        public int CreateIfNotExistsCalls { get; private set; }
        public string? LastBlobName { get; private set; }

        public FakeBlobContainerClient(FakeBlobClient blob) => _blob = blob;

        public override Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(
            PublicAccessType publicAccessType = default,
            IDictionary<string, string> metadata = default!,
            BlobContainerEncryptionScopeOptions encryptionScopeOptions = default!,
            CancellationToken cancellationToken = default)
        {
            CreateIfNotExistsCalls++;
            return Task.FromResult<Response<BlobContainerInfo>>(null!);
        }

        public override BlobClient GetBlobClient(string blobName)
        {
            LastBlobName = blobName;
            return _blob;
        }
    }

    private sealed class FakeBlobServiceClient : BlobServiceClient
    {
        private readonly FakeBlobContainerClient _container;
        public string? LastContainerName { get; private set; }

        public FakeBlobServiceClient(FakeBlobContainerClient container) => _container = container;

        public override BlobContainerClient GetBlobContainerClient(string blobContainerName)
        {
            LastContainerName = blobContainerName;
            return _container;
        }
    }

    private static (BlobStorageService Sut, FakeBlobServiceClient Service,
        FakeBlobContainerClient Container, FakeBlobClient Blob) CreateWithFakes()
    {
        var blob = new FakeBlobClient();
        var container = new FakeBlobContainerClient(blob);
        var service = new FakeBlobServiceClient(container);
        var sut = new BlobStorageService(service, ContainerName, FakeConnectionString);
        return (sut, service, container, blob);
    }

    [Fact]
    public async Task UploadAsync_CreatesContainer_UploadsWithContentType_AndReturnsUri()
    {
        var (sut, service, container, blob) = CreateWithFakes();

        var url = await sut.UploadAsync(
            new MemoryStream(new byte[] { 1, 2, 3 }), "cv.pdf", "application/pdf");

        url.Should().Be(BlobUrl);
        service.LastContainerName.Should().Be(ContainerName);
        container.CreateIfNotExistsCalls.Should().Be(1);
        container.LastBlobName.Should().Be("cv.pdf");
        blob.LastUploadOptions!.HttpHeaders!.ContentType.Should().Be("application/pdf");
        blob.LastUploadStream.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_DeletesBlobExtractedFromUrl()
    {
        var (sut, _, container, blob) = CreateWithFakes();

        await sut.DeleteAsync(BlobUrl);

        container.LastBlobName.Should().Be("cv.pdf");
        blob.DeleteIfExistsCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetSasUrlAsync_ReturnsSignedReadUrl()
    {
        // Real service, fake connection string: GenerateSasUri signs locally,
        // no network involved. Also covers ExtractAccountName/ExtractAccountKey.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureBlobStorage:ConnectionString"] = FakeConnectionString,
                ["AzureBlobStorage:ContainerName"] = ContainerName
            })
            .Build();
        var sut = new BlobStorageService(config);

        var sasUrl = await sut.GetSasUrlAsync(BlobUrl, TimeSpan.FromMinutes(15));

        sasUrl.Should().StartWith(BlobUrl);
        sasUrl.Should().Contain("sig=").And.Contain("sr=b").And.Contain("se=");
    }

    [Fact]
    public void Constructor_MissingConnectionString_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => new BlobStorageService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection string*");
    }
}
