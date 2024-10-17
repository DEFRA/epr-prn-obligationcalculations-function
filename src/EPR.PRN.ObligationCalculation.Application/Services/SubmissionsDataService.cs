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

    public SubmissionsDataService(ILogger<SubmissionsDataService> logger, HttpClient httpClient, IOptions<CommonDataApiConfig> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate)
    {
        _logger.LogInformation("{LogPrefix} >>>>>>> Get Approved Submissions Data from {ApprovedAfterDateString} <<<<<<<<", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix, lastSuccessfulRunDate);
        return await GetSubmissions(lastSuccessfulRunDate);
    }

    private async Task<List<ApprovedSubmissionEntity>> GetSubmissions(string approvedAfterDateString)
    {
        string _submissionsBaseUrl = _config.BaseUrl;
        string _submissionsEndPoint = _config.SubmissionsEndPoint;

        string endpoint = _submissionsBaseUrl + _submissionsEndPoint + approvedAfterDateString;
        _logger.LogInformation("{logPrefix} Fetching Submissions data from: {Endpoint}", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix, endpoint);

        try
        {
            var result = await GetDataAsync(endpoint);
            var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result);
            return submissionEntities ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{LogPrefix} Error while getting submissions data from {Endpoint} : {Ex}", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix, endpoint, ex.Message);
            throw;
        }
    }

    private async Task<string> GetDataAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        return response.StatusCode.HasFlag(HttpStatusCode.OK) ? await response.Content.ReadAsStringAsync() : string.Empty;
    }
}
