using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;

namespace EPR.PRN.ObligationCalculation.Function.Middleware;

[ExcludeFromCodeCoverage(Justification = "Used locally in tests")]
public class BlobStorage(BlobServiceClient blobServiceClient) : IBlobStorage
{
    public async Task<string?> ReadTextFromBlob(string containerName, string blobName)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }

    public async Task WriteTextToBlob(string containerName, string blobName, string content)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        await blobClient.UploadAsync(stream, overwrite: true);
    }
}