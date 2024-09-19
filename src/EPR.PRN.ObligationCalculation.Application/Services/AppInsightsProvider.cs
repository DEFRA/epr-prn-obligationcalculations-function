using System.Text;
using System.Text.Json;
using Azure.Monitor.Query;
using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Application.Services
{
    public class AppInsightsProvider : IAppInsightsProvider
    {
        private readonly ILogger<AppInsightsProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly LogsQueryClient _logsQueryClient;
        private readonly AppInsightsConfig _config;
        private const string Tables = "tables";
        private const string Rows = "rows";
        private const string TimeGenerated = "TimeGenerated";
        public AppInsightsProvider(ILogger<AppInsightsProvider> logger, HttpClient httpClient, LogsQueryClient logsQueryClient, IOptions<AppInsightsConfig> config)
        {
            _logger = logger;
            _httpClient = httpClient;
            _logsQueryClient = logsQueryClient;
            _config = config.Value;
        }

        public async Task<DateTime> GetParameterForApprovedSubmissionsApiCall()
        {
            _logger.LogInformation("{logPrefix} Initiated to retrieve last successful run date", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix);
            var lastSuccessfulRundateFromInsights = await GetLastSuccessfulRunFromInsightsAPI();
            _logger.LogInformation("{logPrefix} Last run date retrieved", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix);

            return lastSuccessfulRundateFromInsights == null
                ? new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                : lastSuccessfulRundateFromInsights.Value;
        }

        private async Task<DateTime?> GetLastSuccessfulRunFromInsights()
        {
            string url = _config.ApiUrl.Replace("{appId}", _config.AppId);

            string logToFind = $"{ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix} COMPLETED";

            string query = $@"{{""query"": ""traces | where message startswith '{logToFind}' | order by timestamp desc | project timestamp | limit 1"" }}";
            var content = new StringContent(query, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string apiResult = await response.Content.ReadAsStringAsync();

            var jsonDocFromApi = JsonDocument.Parse(apiResult);
            var rootElement = jsonDocFromApi.RootElement;
            var tables = rootElement.GetProperty(Tables);
            if (tables.GetArrayLength() == 0 || tables[0].GetProperty(Rows).GetArrayLength() == 0)
            {
                return null;
            }
            return Convert.ToDateTime(tables[0].GetProperty(Rows)[0][0].ToString());
        }

        private async Task<DateTime?> GetLastSuccessfulRunFromInsightsAPI()
        {
            string logToFind = $"{ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix} COMPLETED";
            string query = $@"AppTraces
                         | where Message startswith '{logToFind}'
                         | order by TimeGenerated desc
                         | project TimeGenerated, Message
                         | limit 1";
            var response = await _logsQueryClient.QueryWorkspaceAsync(_config.WorkspaceId, query, TimeSpan.FromDays(1));
            if (response.Value.Table.Rows.Count > 0)
            {
                var row = response.Value.Table.Rows[0];
                return Convert.ToDateTime(row[TimeGenerated].ToString());
            }
            return null;
        }
    }
}
