using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class SubmissionsDataService : ISubmissionsDataService
{
    private readonly ILogger<SubmissionsDataService> _logger;
    private readonly HttpClient _httpClient;
    private readonly CommonDataApiConfig _config;
    private readonly string _logPrefix;

    public SubmissionsDataService(ILogger<SubmissionsDataService> logger, HttpClient httpClient, IOptions<CommonDataApiConfig> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
        _logPrefix = nameof(SubmissionsDataService);
    }

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate)
    {
        _logger.LogInformation("[{LogPrefix}]: Get Approved Submissions Data from {LastSuccessfulRunDate}", _logPrefix, lastSuccessfulRunDate);
        
        string endpoint = _config.SubmissionsEndPoint + lastSuccessfulRunDate;
        _logger.LogInformation("[{logPrefix}]: Fetching Submissions data from: {Endpoint}", _logPrefix, endpoint);

        try
        {
            var result = await GetDataAsync(endpoint);
            var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result);
            return submissionEntities ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Error while getting submissions data from {Endpoint}", _logPrefix, endpoint);
            throw;
        }
    }

    private async Task<string> GetDataAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        return response.StatusCode.HasFlag(HttpStatusCode.OK) ? await response.Content.ReadAsStringAsync() : string.Empty;
    }
}
