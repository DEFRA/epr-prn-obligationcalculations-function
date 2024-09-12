using System.Text;
using System.Text.Json;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ILogger _logger;
        //private readonly ApiClient _apiClient;
        private readonly ISubmissionsDataService _submissionsService;

        private readonly string LogPrefix = "[StoreApprovedSubmissionsFunction]:";
        //private readonly string _submissionsBaseUrl;

        public StoreApprovedSubmissionsFunction(ILoggerFactory loggerFactory, ApiClient apiClient, IConfiguration configuration, ISubmissionsDataService submissionsService)
        {
            _logger = loggerFactory.CreateLogger<StoreApprovedSubmissionsFunction>();
            //_apiClient = apiClient;
            _submissionsService = submissionsService;
            //_submissionsBaseUrl = configuration["SubmissionsBaseUrl"] ?? throw new InvalidOperationException("SubmissionsBaseUrl configuration is missing.");
        }

        [Function("StoreApprovedSubmissionsFunction")]
        public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation(">>>>>>> {LogPrefix} New session started <<<<<<<<", LogPrefix);

            await QueryApplicationInsightsAsync();

            //await GetSubmissions();

            await _submissionsService.GetApprovedSubmissionsData("2024-01-01");
        }

        private static async Task QueryApplicationInsightsAsync()
        {
            string appId = "";
            string apiKey = ""; // You need an API key generated from the Azure portal

            string url = $"https://api.applicationinsights.io/v1/apps/{appId}/query";

            string query = @"{
                        ""query"": ""traces | where message startswith '[StoreApprovedSubmissionsFunction]: COMPLETED' | order by timestamp desc | project timestamp | limit 1""
                    }";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var content = new StringContent(query, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();

                    JsonDocument jsonDoc = JsonDocument.Parse(result);

                    JsonElement root = jsonDoc.RootElement;

                    var parameter = (Convert.ToDateTime((root.GetProperty("tables")[0].GetProperty("rows")[0][0]).ToString())).ToShortDateString();

                    Console.WriteLine($"Extracted timestamp: {parameter}");
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }
            }
        }

        public static DateTime GetCreatedDateFromLogs()
        {
            return DateTime.UtcNow;
        }

        //private async Task GetSubmissions()
        //{
        //    string endpoint = _submissionsBaseUrl + "/v1/pom/approved/" + "2024-01-01";

        //    var result = await _apiClient.GetDataAsync(endpoint);

        //    var approvedSubmissions = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result);

        //    var json = JsonConvert.SerializeObject(approvedSubmissions);

        //    _logger.LogInformation(">>>>>>> Submissions <<<<<<<<");
        //    _logger.LogInformation(json);
        //}
    }
}
