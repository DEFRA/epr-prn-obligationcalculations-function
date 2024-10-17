using Azure.Monitor.Query;
using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class AppInsightsProvider : IAppInsightsProvider
{
    private readonly ILogger<AppInsightsProvider> _logger;
    private readonly LogsQueryClient _logsQueryClient;
    private readonly AppInsightsConfig _config;
    private const string TimeGenerated = "TimeGenerated";
    public AppInsightsProvider(ILogger<AppInsightsProvider> logger, LogsQueryClient logsQueryClient, IOptions<AppInsightsConfig> config)
    {
        _logger = logger;
        _logsQueryClient = logsQueryClient;
        _config = config.Value;
    }

    public async Task<DateTime> GetParameterForApprovedSubmissionsApiCall()
    {
        try
        {
            _logger.LogInformation("{LogPrefix} Initiated to retrieve last successful run date", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix);
            var lastSuccessfulRundateFromInsights = await GetLastSuccessfulRunFromInsights();
            _logger.LogInformation("{LogPrefix} Last run date retrieved", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix);

            return lastSuccessfulRundateFromInsights == null
                ? new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                : lastSuccessfulRundateFromInsights.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{LogPrefix} Error while trying to fetch last run date: {Ex}", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix, ex);
            return new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }

    private async Task<DateTime?> GetLastSuccessfulRunFromInsights()
    {
        string logToFind = $"{ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix} COMPLETED";
        string query = $@"AppTraces
                         | where Message startswith '{logToFind}'
                         | order by TimeGenerated desc
                         | project TimeGenerated
                         | limit 1";
        var response = await _logsQueryClient.QueryWorkspaceAsync(_config.WorkspaceId, query, TimeSpan.FromDays(1));
        if (response?.Value?.Table?.Rows?.Count > 0)
        {
            var row = response.Value.Table.Rows[0];
            return Convert.ToDateTime(row[TimeGenerated].ToString());
        }
        return null;
    }
}
