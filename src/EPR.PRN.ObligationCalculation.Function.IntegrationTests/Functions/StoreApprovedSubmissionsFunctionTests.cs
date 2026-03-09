using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests.Functions;

public class StoreApprovedSubmissionsFunctionTests : IntegrationTestBase
{
    [Fact]
    public async Task WhenInvoked_ShouldCalculateObligation()
    {
        await CommonDataApiStub.HasObligations();
        await PrnApiStub.HasCalculate();
        
        await FunctionContext.Invoke(Function.Functions.StoreApprovedSubmissionsFunction);
        
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await PrnApiStub.GetCalculateRequests();
            requests.Count.Should().Be(1);

            var request = requests[0];
            var json = JsonDocument.Parse(request.Request.Body!);

            var submissions = json.RootElement.EnumerateArray().ToList();
            submissions.Count.Should().Be(1);

            var submission = submissions[0];

            submission.GetProperty("organisationId").GetString().Should().Be("187fa134-0376-4c65-b670-7dd794297a63");
        });
    }
}