using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class SubmissionsDataService : ISubmissionsDataService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger _logger;
    private readonly string _submissionsBaseUrl;

    public SubmissionsDataService(ILoggerFactory loggerFactory, ApiClient apiClient, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<SubmissionsDataService>();
        _apiClient = apiClient;
        //_submissionsBaseUrl = configuration["SubmissionsBaseUrl"] ?? throw new InvalidOperationException("SubmissionsBaseUrl configuration is missing.");
    }

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string approvedAfterDateString)
    {
        const string logMessageTemplate = ">>>>>>> Get Approved Submissions Data from {ApprovedAfterDateString} <<<<<<<<";
        _logger.LogInformation(logMessageTemplate, approvedAfterDateString);

        return await GetSubmissions(approvedAfterDateString);
    }

    private async Task<List<ApprovedSubmissionEntity>> GetSubmissions(string approvedAfterDateString)
    {
        string endpoint = _submissionsBaseUrl + "/v1/pom/approved/" + approvedAfterDateString;
        _logger.LogInformation("Fetching Submissions data from: {Endpoint}", endpoint);

        try
        {
            var result = await _apiClient.GetDataAsync(endpoint);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No submissions data found for {ApprovedAfterDateString}", approvedAfterDateString);
                return new List<ApprovedSubmissionEntity>();
            }
            else
            {
                _logger.LogInformation("Submissions data: {Result}", result);

                return JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting submissions data from {Endpoint} : {Ex}", endpoint, ex.Message);
            throw;
        }
    }
}
