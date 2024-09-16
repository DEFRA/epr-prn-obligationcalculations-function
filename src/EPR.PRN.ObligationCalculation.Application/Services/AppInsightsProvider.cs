using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Monitor.Query;
using EPR.PRN.ObligationCalculation.Application.Configs;

namespace EPR.PRN.ObligationCalculation.Application.Services
{
    public class AppInsightsProvider : IAppInsightsProvider
    {
        public AppInsightsProvider()
        {
        }

        public async Task<DateTime> GetParameterForApprovedSubmissionsApiCall()
        {
            DateTime? lastSuccessfulRundateFromInsights = await GetLastSuccessfulRunFromInsights();

            return lastSuccessfulRundateFromInsights == null
                ? new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                : lastSuccessfulRundateFromInsights.Value;
        }

        private static async Task<DateTime?> GetLastSuccessfulRunFromInsights()
        {
            string appId = "d0f1af04-0bf9-4e1e-bb4d-3a4b9d40cf04";
            string apiKey = "j2ttlxmuqd82cl0m1u7vgeygy05k8k8n4ctmb6k6";
            string url = $"https://api.applicationinsights.io/v1/apps/{appId}/query";

            string logToFind = $"{ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix} COMPLETED";

            string query = $@"{{""query"": ""traces | where message startswith '{logToFind}' | order by timestamp desc | project timestamp | limit 1"" }}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var content = new StringContent(query, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string apiResult = await response.Content.ReadAsStringAsync();

                JsonDocument jsonDocFromApi = JsonDocument.Parse(apiResult);
                JsonElement rootElement = jsonDocFromApi.RootElement;

                if (rootElement.GetProperty("tables")[0].GetProperty("rows").GetArrayLength() == 0)
                {
                    return null;
                }

                return Convert.ToDateTime(rootElement.GetProperty("tables")[0].GetProperty("rows")[0][0].ToString());
            }
        }

        private static async Task<DateTime?> GetLastSuccessfulRunFromInsightsAPI()
        {
            string clientId = "dd6d3e95-eef6-48e8-84cd-05321152c24f";
            string tenantId = "6f504113-6b64-43f2-ade9-242e05780007";
            string clientSecret = "<your-client-secret>";
            string workspaceId = "4d0d277f-63d3-49e3-a58a-62e89896356d";

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var logsClient = new LogsQueryClient(credential);

            string logToFind = $"{ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix} COMPLETED";
            string query = $@"AppTraces
                         | where Message startswith '{logToFind}'
                         | project TimeGenerated
                         | order by TimeGenerated desc
                         | limit 1";
            var response = await logsClient.QueryWorkspaceAsync(workspaceId, query, TimeSpan.FromDays(1));
            if (response.Value.Table.Rows.Count > 0)
            {
                var row = response.Value.Table.Rows[0];
                return Convert.ToDateTime(row["TimeGenerated"].ToString());
            }
            return null;
        }
    }
}
