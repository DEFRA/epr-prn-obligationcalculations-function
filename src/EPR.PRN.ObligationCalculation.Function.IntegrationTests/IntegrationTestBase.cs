using EPR.PRN.ObligationCalculation.Function.IntegrationTests.Stubs;
using Xunit;

namespace EPR.PRN.ObligationCalculation.Function.IntegrationTests;

[Trait("Category", "IntegrationTest")]
[Collection("Integration Tests")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected CommonDataApi CommonDataApiStub = null!;
    protected PrnApi PrnApiStub = null!;
    
    private WireMockContext _wireMockContext = null!;

    public async Task InitializeAsync()
    {
        _wireMockContext = new WireMockContext();

        await _wireMockContext.InitializeAsync();

        CommonDataApiStub = new CommonDataApi(_wireMockContext);
        PrnApiStub = new PrnApi(_wireMockContext);
    }

    public async Task DisposeAsync()
    {
        await _wireMockContext.DisposeAsync();
    }
}
