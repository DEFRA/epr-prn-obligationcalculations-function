#nullable disable

using EPR.PRN.ObligationCalculation.Application.Services;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests;

[ExcludeFromCodeCoverage]
[TestClass]
public class AppInsightsProviderTests
{
    Mock<IAppInsightsProvider> appInsightsProvider;
    DateTime dateTime;

    [TestInitialize]
    public void TestInitialize()
    {
        dateTime = DateTime.Now;
        appInsightsProvider = new Mock<IAppInsightsProvider>();
        appInsightsProvider.Setup(x => x.GetParameterForApprovedSubmissionsApiCall()).ReturnsAsync(dateTime);
    }

    [TestMethod]
    public async Task GetParameterForApprovedSubmissionsApiCall_WhenCalled_ShouldReturnLastSubmissionsDate()
    {
        // Arrange
        var service = appInsightsProvider.Object;

        // Act
        var result = await service.GetParameterForApprovedSubmissionsApiCall();

        // Assert
        appInsightsProvider.Verify(x => x.GetParameterForApprovedSubmissionsApiCall(), Times.Once);
        Assert.IsNotNull(result);
        Assert.AreEqual(dateTime, result);
    }
}
