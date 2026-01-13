using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace EPR.PRN.ObligationCalculation.Function.Services;

public interface IEprPrnCommonBackendService
{
    Task CalculateApprovedSubmission(string submissions);
}

public class EprPrnCommonBackendService(ILogger<EprPrnCommonBackendService> logger, HttpClient httpClient, IOptions<PrnServiceApiConfig> config) : IEprPrnCommonBackendService
{
    public async Task CalculateApprovedSubmission(string submissions)
    {
        var rawEndpoint = "api/v1/prn/organisation/{0}/calculate";
        try
        {
            if (string.IsNullOrEmpty(submissions))
            {
                logger.LogInformation("{LogPrefix}: EprPrnCommonBackendService - CalculateApprovedSubmission - Submissions message is empty", config.Value.LogPrefix);
            }
            else
            {
                var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(submissions);
                if (submissionEntities != null)
                {
                    var submitterId = submissionEntities[0].SubmitterId;
                    string endpoint = string.Format(rawEndpoint, submitterId);
                    logger.LogInformation("{LogPrefix}: EprPrnCommonBackendService - CalculateApprovedSubmission - Submissions request being sent to Endpoint: {Endpoint}, SubmitterId: {SubmitterId}, Entity Count: {Count} ", config.Value.LogPrefix, endpoint, submitterId, submissionEntities.Count);

                    var response = await httpClient.PostAsJsonAsync(endpoint, submissionEntities);
                    logger.LogInformation("{LogPrefix}: EprPrnCommonBackendService - CalculateApprovedSubmission - Calculate endpoint execution completed with status code - {StatusCode}", config.Value.LogPrefix, response.StatusCode);
                    
                    response.EnsureSuccessStatusCode();
                    logger.LogInformation("{LogPrefix}: EprPrnCommonBackendService - CalculateApprovedSubmission - Submissions message is posted to backend successfully", config.Value.LogPrefix);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: EprPrnCommonBackendService - CalculateApprovedSubmission - Error while submitting submissions data to endpoint {Endpoint}", config.Value.LogPrefix, rawEndpoint);
            throw;
        }
    }
}