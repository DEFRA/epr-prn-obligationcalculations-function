using System.Net;
using AwesomeAssertions;
using Xunit;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests.Functions;

public class HealthCheckTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenHealthCheckRequest_ShouldBeSuccess()
    {
        var result = await FunctionContext.Get("/api/health");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}