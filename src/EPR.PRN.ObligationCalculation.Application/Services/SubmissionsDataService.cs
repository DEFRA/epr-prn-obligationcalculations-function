using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class SubmissionsDataService : ISubmissionsDataService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    public SubmissionsDataService(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<SubmissionsDataService>();
        _configuration = configuration;
    }

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string approvedAfterDateString)
    {
        const string logMessageTemplate = ">>>>>>> Get Approved Submissions Data from {ApprovedAfterDateString} <<<<<<<<";
        _logger.LogInformation(logMessageTemplate, approvedAfterDateString);

        return await GetSubmissions(approvedAfterDateString);
    }

    private async Task<List<ApprovedSubmissionEntity>> GetSubmissions(string approvedAfterDateString)
    {
        string _submissionsBaseUrl = _configuration["SubmissionsBaseUrl"] ?? throw new InvalidOperationException("SubmissionsBaseUrl configuration is missing.");
        string _submissionsEndPoint = _configuration["SubmissionsEndPoint"] ?? throw new InvalidOperationException("SubmissionsEndPoint configuration is missing.");

        string endpoint = _submissionsBaseUrl + _submissionsEndPoint + approvedAfterDateString;
        _logger.LogInformation("Fetching Submissions data from: {Endpoint}", endpoint);

        try
        {
            var result = await GetDataAsync(endpoint);

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

    private static async Task<string> GetDataAsync(string endpoint)
    {
        var _httpClient = new HttpClient();
        var response = await _httpClient.GetAsync(endpoint);

        return response.StatusCode.HasFlag(HttpStatusCode.OK) ? await response.Content.ReadAsStringAsync() : string.Empty;
    }
}
