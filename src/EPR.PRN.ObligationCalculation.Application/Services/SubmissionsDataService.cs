using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class SubmissionsDataService(ILogger<SubmissionsDataService> logger, HttpClient httpClient, IOptions<SubmissionsServiceApiConfig> config) : ISubmissionsDataService
{
    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate)
    {
        var endpoint = config.Value.SubmissionsEndPoint + lastSuccessfulRunDate;
        logger.LogInformation("{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Fetching Submissions data from: {Endpoint}", config.Value.LogPrefix, endpoint);

        try
        {
            var response = await httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Received approved submissions data from: {Endpoint}", config.Value.LogPrefix, endpoint);
            
            return JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(content) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: SubmissionsDataService - GetApprovedSubmissionsData - Error while getting submissions data from {Endpoint}", config.Value.LogPrefix, endpoint);
            throw;
        }
    }
}
