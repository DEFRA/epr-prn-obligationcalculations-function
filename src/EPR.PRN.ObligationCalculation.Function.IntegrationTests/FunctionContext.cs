using System.Globalization;
using System.Text;
using Azure.Storage.Blobs;
using EPR.PRN.ObligationCalculation.Function.Middleware;
using AwesomeAssertions;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests;

public static class FunctionContext
{
    private static readonly HttpClient HttpClient;
    private static readonly BlobStorage BlobStorage;

    static FunctionContext()
    {
        HttpClient = new HttpClient { BaseAddress = new Uri($"{BaseUri}/admin/functions/") };
        HttpClient.DefaultRequestHeaders.Add("x-functions-key", "this-is-a-dummy-value");
        
        // This connection string is the well-known default credential for the Azurite storage emulator.
        // It is NOT a sensitive secret - it's a hardcoded value built into Azurite for local development.
        // See: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite#well-known-storage-account-and-key
        const string connectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;";
        
        var blobServiceClient = new BlobServiceClient(connectionString);
        
        BlobStorage = new BlobStorage(blobServiceClient);
    }

    private static string BaseUri => "http://localhost:7234";

    public static async Task Invoke(string functionName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, functionName)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        var utcNow = DateTime.UtcNow;
        var response = await HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        // See FunctionRunningMiddleware that sets the last run date time
        // for a specific function. This will block until the function
        // has finished, or if the maximum WaitForAsync time is reached.
        await AsyncWaiter.WaitForAsync(async () =>
            {
                var lastRun = await GetLastRun(functionName);
                lastRun.Should().BeAfter(utcNow);
            },
            delay: TimeSpan.FromMilliseconds(10));
    }

    public static async Task<HttpResponseMessage> Get(string requestUri) =>
        await HttpClient.GetAsync(requestUri);

    private static async Task<DateTime> GetLastRun(string functionName)
    {
        var content = await BlobStorage.ReadTextFromBlob(FunctionRunningMiddleware.ContainerName, $"{functionName}.txt");

        return string.IsNullOrEmpty(content)
            ? DateTime.MinValue
            : DateTime.ParseExact(content, "O", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}