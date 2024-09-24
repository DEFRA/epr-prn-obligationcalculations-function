#nullable disable
using Azure;
using Azure.Monitor.Query;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Application.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class AppInsightsProviderTests
{
    private Mock<ILogger<AppInsightsProvider>> _loggerMock;
    private Mock<LogsQueryClient> _logsQueryClientMock;
    private Mock<IOptions<AppInsightsConfig>> _configMock;
    private AppInsightsConfig _config;
    private AppInsightsProvider _appInsightsProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<AppInsightsProvider>>();
        _logsQueryClientMock = new Mock<LogsQueryClient>();
        _config = new AppInsightsConfig { WorkspaceId = "dummy-workspace-id" };
        _configMock = new Mock<IOptions<AppInsightsConfig>>();
        _configMock.Setup(c => c.Value).Returns(_config);

        _appInsightsProvider = new AppInsightsProvider(_loggerMock.Object, _logsQueryClientMock.Object, _configMock.Object);
    }

    [TestMethod]
    public async Task GetParameterForApprovedSubmissionsApiCall_ShouldReturnLastSuccessfulRunDate()
    {
        // Arrange
        var timeGenerated = new DateTime(2024, 9, 23, 0, 0, 0, DateTimeKind.Utc);
        var logsQueryResult = MonitorQueryModelBuilder.CreateMockLogsQueryResult(timeGenerated);

        _logsQueryClientMock
            .Setup(c => c.QueryWorkspaceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<QueryTimeRange>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(logsQueryResult, null));

        // Act
        var result = await _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall();

        // Assert
        Assert.AreEqual(timeGenerated.Date, result.Date);
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
    }

    [TestMethod]
    public async Task GetParameterForApprovedSubmissionsApiCall_ShouldReturnDefaultDate_WhenNoRunDateFound()
    {
        // Arrange
        var logsQueryResult = MonitorQueryModelBuilder.CreateMockLogsQueryResult(null);
        _logsQueryClientMock
            .Setup(c => c.QueryWorkspaceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<QueryTimeRange>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(logsQueryResult, null));

        // Act
        var result = await _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall();

        // Assert
        Assert.AreEqual(new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public async Task GetParameterForApprovedSubmissionsApiCall_ShouldLogError_WhenExceptionThrown()
    {
        // Arrange
        _logsQueryClientMock
            .Setup(client => client.QueryWorkspaceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<QueryTimeRange>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Some error"));

        // Act
        var result = await _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall();

        // Assert
        Assert.AreEqual(new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}

