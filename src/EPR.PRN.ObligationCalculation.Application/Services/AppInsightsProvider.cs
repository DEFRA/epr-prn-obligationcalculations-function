using System.Text;
using System.Text.Json;
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

            if (lastSuccessfulRundateFromInsights == null)
            {
                return new DateTime(DateTime.Now.Year, 1, 1);
            }
            else
            {
                return lastSuccessfulRundateFromInsights.Value;
            }
        }

        private async Task<DateTime?> GetLastSuccessfulRunFromInsights()
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

                if (!response.IsSuccessStatusCode || response == null)
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
    }
}
