using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Application.Services
{
    public class CommonDataService : ICommonDataService
    {
        private readonly ApiClient _httpClient;
        private readonly CommonDataApiConfig _apiConfig;
        private readonly ILogger<CommonDataService> _logger;

        public CommonDataService(ApiClient httpClient, IOptions<CommonDataApiConfig> apiConfig, ILogger<CommonDataService> logger)
        {
            _httpClient = httpClient;
            _apiConfig = apiConfig.Value;
            _logger = logger;
        }

        public Task<HttpResponseMessage> GetApprovedSubmissionsWithAggregatedPomData(string approvedAfterDateString)
        {
            throw new NotImplementedException();
        }

        //public async Task<HttpResponseMessage> GetApprovedSubmissionsWithAggregatedPomData(string approvedAfterDateString)
        //{
        //    return new NotImplementedException();
        //    //return await _httpClient.GetAsync($"{_apiConfig.Endpoint}{approvedAfterDateString}");
        //}
    }
}
