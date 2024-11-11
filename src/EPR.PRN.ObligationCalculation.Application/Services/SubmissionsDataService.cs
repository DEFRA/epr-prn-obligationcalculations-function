using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class SubmissionsDataService(ILogger<SubmissionsDataService> logger, HttpClient httpClient, IOptions<CommonDataApiConfig> config) : ISubmissionsDataService
{

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate)
    {
        logger.LogInformation("{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Get Approved Submissions Data from {LastSuccessfulRunDate}", config.Value.LogPrefix, lastSuccessfulRunDate);
        
        string endpoint = config.Value.SubmissionsEndPoint + lastSuccessfulRunDate;
        logger.LogInformation("{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Fetching Submissions data from: {Endpoint}", config.Value.LogPrefix, endpoint);

        try
        {
            var result = await GetDataAsync(endpoint);
            logger.LogInformation("{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Received approved submissions from data API {Result}", config.Value.LogPrefix, JsonConvert.SerializeObject(result));
            var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result);
            return submissionEntities ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Error while getting submissions data from {Endpoint}", config.Value.LogPrefix, endpoint);
            throw;
        }
    }

    private async Task<string> GetDataAsync(string endpoint)
    {
        var response = await httpClient.GetAsync(endpoint);
        return response.StatusCode.HasFlag(HttpStatusCode.OK) ? await response.Content.ReadAsStringAsync() : string.Empty;
    }
}
