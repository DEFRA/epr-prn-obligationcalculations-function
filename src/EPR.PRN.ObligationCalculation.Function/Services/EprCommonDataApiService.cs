using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Function.Services;

public interface IEprCommonDataApiService
{
    Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate);
}

public class EprCommonDataApiService(ILogger<EprCommonDataApiService> logger, HttpClient httpClient, IOptions<SubmissionsServiceApiConfig> config) : IEprCommonDataApiService
{

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate)
    {
        logger.LogInformation("{LogPrefix}: EprCommonDataApiService - GetApprovedSubmissionsData - Get Approved Submissions Data from {LastSuccessfulRunDate}", config.Value.LogPrefix, lastSuccessfulRunDate);

        string endpoint = $"api/submissions/v1/pom/approved/{lastSuccessfulRunDate}";
        logger.LogInformation("{LogPrefix}: EprCommonDataApiService - GetApprovedSubmissionsData - Fetching Submissions data from: {Endpoint}", config.Value.LogPrefix, endpoint);

        try
        {
            var response = await httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            logger.LogInformation("{LogPrefix}: EprCommonDataApiService - GetApprovedSubmissionsData - Received approved submissions data from: {Endpoint}", config.Value.LogPrefix, endpoint);
            var submissionEntities = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result);
            return submissionEntities ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: EprCommonDataApiService - GetApprovedSubmissionsData - Error while getting submissions data from {Endpoint}", config.Value.LogPrefix, endpoint);
            throw;
        }
    }
}
