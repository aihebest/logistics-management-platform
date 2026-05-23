using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LogisticsApi.Models.DTOs;

namespace LogisticsApi.Services;

public interface IStorageService
{
    Task<SasTokenDto> GenerateUploadSasAsync(string container, string fileName);
    Task<string> UploadAsync(string container, string fileName, Stream content, string contentType);
    Task DeleteAsync(string container, string blobName);
}

public class StorageService(BlobServiceClient blobClient, IConfiguration config) : IStorageService
{
    public async Task<SasTokenDto> GenerateUploadSasAsync(string container, string fileName)
    {
        var containerName = config[$"Storage:BlobContainers:{container}"] ?? container.ToLower();
        var containerClient = blobClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient2 = containerClient.GetBlobClient(blobName);
        var expiry = DateTimeOffset.UtcNow.AddHours(1);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiry
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUri = blobClient2.GenerateSasUri(sasBuilder);
        return new SasTokenDto(sasUri.ToString(), blobName, expiry);
    }

    public async Task<string> UploadAsync(string container, string fileName, Stream content, string contentType)
    {
        var containerName = config[$"Storage:BlobContainers:{container}"] ?? container.ToLower();
        var containerClient = blobClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blob = containerClient.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string container, string blobName)
    {
        var containerName = config[$"Storage:BlobContainers:{container}"] ?? container.ToLower();
        var blob = blobClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync();
    }
}
