using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ILogger _logger;
        private static string LogPrefix = "[StoreApprovedSubmissionsFunction]:";
        private DateTime? CreatedDate = null;

        public StoreApprovedSubmissionsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StoreApprovedSubmissionsFunction>();
        }

        [Function("StoreApprovedSubmissionsFunction")]
        //public async Task RunAsync([TimerTrigger("0 0 12 * * 1-5")] TimerInfo myTimer)
        public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            //DateTime? lastSuccessfulRundateFromInsights = GetLastSuccessfulRunFromInsights();
            //DateTime? runDate = null;

            //if (lastSuccessfulRundateFromInsights == null)
            //{
            //    runDate = new DateTime(DateTime.Now.Year, 1, 1);
            //}
            //else
            //{
            //    runDate = lastSuccessfulRundateFromInsights.Value;
            //}

            await QueryApplicationInsightsAsync();

            //_logger.LogInformation($"{LogPrefix} New session started");

            //if (myTimer.ScheduleStatus is not null)
            //{
            //    _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            //}
        }

        private DateTime GetLastSuccessfulRunFromInsights()
        {
            throw new NotImplementedException();
        }

        //private static async Task RetrieveLogsFromAppInsights()
        //{
        //    string workspaceId = "4d0d277f-63d3-49e3-a58a-62e89896356d";
        //    var logsClient = new LogsQueryClient(new DefaultAzureCredential());
        //    string query = @"AppTraces
        //         | where Message contains 'found' 
        //         | project TimeGenerated, Message
        //         | order by TimeGenerated desc
        //         | limit 1";

        //    // Time range for the logs query
        //    TimeSpan queryTimeRange = TimeSpan.FromDays(1);

        //    // Execute the query
        //    Response<LogsQueryResult> logResponse = await logsClient.QueryWorkspaceAsync
        //    (
        //        workspaceId,
        //        query,
        //        new QueryTimeRange(queryTimeRange)
        //    );

        //    // Parse the result and display logs
        //    LogsTable table = logResponse.Value.AllTables[0];
        //    var timeGenerated = table.Rows[0]["TimeGenerated"].ToString();
        //    Console.WriteLine($"Time Generated: {timeGenerated}");
        //}

        private static async Task QueryApplicationInsightsAsync()
        {
            string appId = "";
            string apiKey = ""; // You need an API key generated from the Azure portal

            string url = $"https://api.applicationinsights.io/v1/apps/{appId}/query";

            //var query = @"{""query"": ""traces            
            //| where message startswith 'COMPLETED'
            //| order by timestamp desc
            //| project message
            //| take 1""}";

            //    string query = @"{
            //    ""query"": ""traces | where timestamp > ago(1d) | where message startswith 'COMPLETED' | limit 1""
            //}";

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

                    // Navigate to the rows section and extract the timestamp value
                    JsonElement root = jsonDoc.RootElement;

                    var parameter = (Convert.ToDateTime((root.GetProperty("tables")[0].GetProperty("rows")[0][0]).ToString())).ToShortDateString();

                    //var timestamp = root.GetProperty("tables")[0]   // Get the first table
                    //                 .GetProperty("rows")[0]     // Get the first row
                    //               .GetProperty("timestamp")[0]             // Get the first element in that row (the timestamp)
                    //             .GetString();               // Extract the string value

                    // Output the extracted timestamp
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
    }
}
