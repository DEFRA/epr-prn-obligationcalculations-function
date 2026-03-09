using System.Net;
using WireMock.Client.Extensions;
using Xunit;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests.Stubs;

public class CommonDataApi(WireMockContext wireMock)
{
    public async Task HasObligations()
    {
        var mappingBuilder = wireMock.WireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(builder =>
            builder
                .WithRequest(request => request.UsingGet().WithPath("/api/submissions/v1/pom/approved/*"))
                .WithResponse(response =>
                    response
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithBodyAsJson(
                            new[]
                            {
                                new
                                {
                                    submissionPeriod = "2025",
                                    packagingMaterial = "FC",
                                    packagingMaterialWeight = 16,
                                    organisationId = "187fa134-0376-4c65-b670-7dd794297a63",
                                    submitterId = "aea242e0-ecaa-49b3-acdb-67ea3274b862",
                                    submitterType = "ComplianceScheme",
                                    numberOfDaysObligated = (object?)null
                                }
                            }
                        )
                )
        );

        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status.Guid);
    }
}
