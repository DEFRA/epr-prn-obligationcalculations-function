namespace EPR.PRN.ObligationCalculation.Function.Middleware;

public interface IBlobStorage
{
    Task<string?> ReadTextFromBlob(string containerName, string blobName);
    Task WriteTextToBlob(string containerName, string blobName, string content);
}