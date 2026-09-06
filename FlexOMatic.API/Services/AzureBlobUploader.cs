using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class AzureBlobUploader
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobUploader(string connectionString, string containerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadCropImageAsync(byte[] imageBytes, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        using var stream = new MemoryStream(imageBytes);

        var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders
        {
            ContentType = "image/png"
        };

        await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = headers
        });

        return blobClient.Uri.ToString(); // ✅ Public URL for Roblox
    }

}
