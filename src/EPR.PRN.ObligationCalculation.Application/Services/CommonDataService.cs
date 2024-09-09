using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Application.Services
{
    public class CommonDataService : ICommonDataService
    {
        private readonly HttpClient _httpClient;
        private readonly CommonDataApiConfig _apiConfig;
        private readonly ILogger<CommonDataService> _logger;

        public CommonDataService(HttpClient httpClient, IOptions<CommonDataApiConfig> apiConfig, ILogger<CommonDataService> logger)
        {
            _httpClient = httpClient;
            _apiConfig = apiConfig.Value;
            _logger = logger;
        }
        public async Task<HttpResponseMessage> GetApprovedSubmissionsWithAggregatedPomData(string approvedAfterDateString)
        {
            return await _httpClient.GetAsync($"{_apiConfig.Endpoint}{approvedAfterDateString}");
        }
    }
}
