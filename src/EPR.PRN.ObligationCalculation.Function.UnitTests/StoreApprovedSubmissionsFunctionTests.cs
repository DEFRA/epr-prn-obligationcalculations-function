using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests;

[TestClass]
public class StoreApprovedSubmissionsFunctionTests
{
    private Mock<ISubmissionsDataService> _submissionsDataService = null!;
    private Mock<ILogger<StoreApprovedSubmissionsFunction>> _loggerMock = null!;
    private Mock<IServiceBusProvider> _serviceBusProviderMock = null!;
    private Mock<IOptions<ApplicationConfig>> _configMock = null!;
    private StoreApprovedSubmissionsFunction _function = null!;
    private TimerInfo _timerInfo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _timerInfo = new TimerInfo();
        _loggerMock = new Mock<ILogger<StoreApprovedSubmissionsFunction>>();
        _submissionsDataService = new Mock<ISubmissionsDataService>();
        _serviceBusProviderMock = new Mock<IServiceBusProvider>();

        _configMock = new Mock<IOptions<ApplicationConfig>>();
        var config = new ApplicationConfig
        {
            DefaultRunDate = "2024-01-01",
            FunctionIsEnabled = true
        };

        _configMock.Setup(c => c.Value).Returns(config);
    }

    [TestMethod]
    public async Task RunAsync_FunctionIsDisabled_ShouldLog()
    {
        var config = new ApplicationConfig
        {
            FunctionIsEnabled = false
        };

        _configMock.Setup(x => x.Value).Returns(config);

        // Arrange
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object,
            _configMock.Object);

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    [TestMethod]
    [DataRow("", 3)]
    [DataRow(null, 3)]
    [DataRow("2024-10-10", 3)]
    public async Task RunAsync_ShouldSendApprovedSubmissionsToQueue_WhenSubmissionsSentToQueue(string lastSuccessfulRunDateFromQueue, int logInformationCount)
    {
        // Arrange
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object,
            _configMock.Object);

        // Act
        await _function.RunAsync(_timerInfo);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(logInformationCount));
    }

    [TestMethod]
    public async Task RunAsync_ShouldHandleException_WhenErrorOccurs()
    {
        // Arrange
        var expectedErrorMessagePart = "StoreApprovedSubmissionsFunction: Ended with error while storing approved submission";

		_function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object,
            _configMock.Object);

        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test Exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _function.RunAsync(_timerInfo));

        // Assert - Ex
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedErrorMessagePart)),
			It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}