using System.Net;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client.Extensions;
using Xunit;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests.Stubs;

public class PrnApi(WireMockContext wireMock)
{
    public async Task HasCalculate()
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request =>
                    request.UsingPost()
                        .WithPath("/api/v1/prn/organisation/aea242e0-ecaa-49b3-acdb-67ea3274b862/calculate"))
                .WithResponse(response => response.WithStatusCode(HttpStatusCode.Accepted))
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }

    public async Task<IList<LogEntryModel>> GetCalculateRequests()
    {
        var requestModel = new RequestModel
        {
            Methods = ["POST"],
            Path = "/api/v1/prn/organisation/aea242e0-ecaa-49b3-acdb-67ea3274b862/calculate"
        };
        
        return await wireMock.WireMockAdminApi.FindRequestsAsync(requestModel);
    }
}
