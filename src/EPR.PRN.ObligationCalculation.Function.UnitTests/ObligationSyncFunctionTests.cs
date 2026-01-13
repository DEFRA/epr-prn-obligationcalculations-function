using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Function.Services;
using EPR.PRN.ObligationCalculation.Function.UnitTests.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests;

[TestClass]
public class ObligationSyncFunctionTests
{
    private Mock<ILogger<ObligationSyncFunction>> _mockLogger = null!;
    private Mock<IEprCommonDataApiService> _mockEprCommonDataApiService = null!;
    private Mock<IEprPrnCommonBackendService> _mockEprPrnCommonBackendService = null!;
    private Mock<IServiceBusProvider> _mockServiceBusProvider = null!;
    private Mock<IOptions<ApplicationConfig>> _mockConfig = null!;
    private ObligationSyncFunction _underTest = null!;
    private TimerInfo _timerInfo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<ObligationSyncFunction>>();
        _timerInfo = new TimerInfo();
        _mockEprCommonDataApiService = new Mock<IEprCommonDataApiService>();
        _mockEprPrnCommonBackendService = new Mock<IEprPrnCommonBackendService>();
        _mockServiceBusProvider = new Mock<IServiceBusProvider>();

        _mockConfig = new Mock<IOptions<ApplicationConfig>>();
        var config = new ApplicationConfig
        {
            DefaultRunDate = "2024-01-01",
            FunctionIsEnabled = true
        };

        _mockConfig.Setup(c => c.Value).Returns(config);

        var mockApprovedSubmissions = new List<ApprovedSubmissionEntity>
        {
            new ApprovedSubmissionEntity
            {
                OrganisationId = Guid.NewGuid(),
                SubmissionPeriod = "2025-Q1",
                PackagingMaterial = "Plastic",
                PackagingMaterialWeight = 100,
                SubmitterId = Guid.NewGuid(),
                SubmitterType = "Producer"
            }
        };
        _mockEprCommonDataApiService
            .Setup(s => s.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ReturnsAsync(mockApprovedSubmissions);

        _underTest = new ObligationSyncFunction(
            _mockLogger.Object,
            _mockEprCommonDataApiService.Object,
            _mockEprPrnCommonBackendService.Object,
            _mockServiceBusProvider.Object,
            _mockConfig.Object);

    }

    // -------------------------- PUBLISH --------------------------

    [TestMethod]
    public async Task PublishAsync_underTestIsDisabled_ShouldLog()
    {
        var config = new ApplicationConfig
        {
            FunctionIsEnabled = false
        };

        _mockConfig.Setup(x => x.Value).Returns(config);

        // Arrange
        _underTest = new ObligationSyncFunction(
            _mockLogger.Object,
            _mockEprCommonDataApiService.Object,
            _mockEprPrnCommonBackendService.Object,
            _mockServiceBusProvider.Object,
            _mockConfig.Object);

        _mockServiceBusProvider.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync("2024-10-10");

        // Act
        await _underTest.PublishAsync(_timerInfo);

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    [TestMethod]
    [DataRow("", 5)]
    [DataRow(null, 5)]
    [DataRow("2024-10-10", 5)]
    public async Task PublishAsync_ShouldSendApprovedSubmissionsToQueue_WhenSubmissionsSentToQueue(string lastSuccessfulRunDateFromQueue, int logInformationCount)
    {
        // Arrange
        _underTest = new ObligationSyncFunction(
            _mockLogger.Object,
            _mockEprCommonDataApiService.Object,
            _mockEprPrnCommonBackendService.Object,
            _mockServiceBusProvider.Object,
            _mockConfig.Object);

        _mockServiceBusProvider.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync(lastSuccessfulRunDateFromQueue);

        // Act
        await _underTest.PublishAsync(_timerInfo);

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(logInformationCount));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task PublishAsync_Terminated_WhenRunDateIsNullOrEmpty(string lastSuccessfulRunDate)
    {
        // Arrange
        _mockConfig.Object.Value.DefaultRunDate = lastSuccessfulRunDate;
        _underTest = new ObligationSyncFunction(
            _mockLogger.Object,
            _mockEprCommonDataApiService.Object,
            _mockEprPrnCommonBackendService.Object,
            _mockServiceBusProvider.Object,
            _mockConfig.Object);

        _mockServiceBusProvider.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync(lastSuccessfulRunDate);

        // Act
        await _underTest.PublishAsync(_timerInfo);

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task PublishAsync_ShouldHandleException_WhenErrorOccurs()
    {
        // Arrange
        var expectedErrorMessagePart = "StoreApprovedSubmissionsFunction: Ended with error while storing approved submission";

		_underTest = new ObligationSyncFunction(
            _mockLogger.Object,
            _mockEprCommonDataApiService.Object,
            _mockEprPrnCommonBackendService.Object,
            _mockServiceBusProvider.Object,
            _mockConfig.Object);

        var lastSuccessfulRunDate = "2024-10-10";
        _mockServiceBusProvider.Setup(x => x.GetLastSuccessfulRunDateFromQueue()).ReturnsAsync(lastSuccessfulRunDate);

        _mockEprCommonDataApiService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test Exception"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _underTest.PublishAsync(_timerInfo));

        // Assert - Ex
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedErrorMessagePart)),
			It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // -------------------------- PROCESS --------------------------

    [TestMethod]
    public async Task ProcessAsync_underTestIsEnabled_ShouldProcessMessageSuccessfully()
    {
        var config = new ApplicationConfig
        {
            FunctionIsEnabled = true
        };

        _mockConfig.Setup(x => x.Value).Returns(config);

        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _underTest.ProcessAsync(serviceBusMessage);

        // Assert
        _mockEprPrnCommonBackendService.Verify(service => service.CalculateApprovedSubmission("test-message-body"), Times.Once);
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ProcessAsync_underTestIsDisabled_ShouldLog()
    {
        var config = new ApplicationConfig
        {
            FunctionIsEnabled = false
        };

        _mockConfig.Setup(x => x.Value).Returns(config);

        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _underTest.ProcessAsync(serviceBusMessage);

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task ProcessAsync_ShouldProcessMessageSuccessfully()
    {
        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _underTest.ProcessAsync(serviceBusMessage);

        // Assert
        _mockEprPrnCommonBackendService.Verify(service => service.CalculateApprovedSubmission("test-message-body"), Times.Once);
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ProcessAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");
        var expectedErrorMessagePart = "ProcessApprovedSubmissionsFunction: Exception occurred while processing message";

		_mockEprPrnCommonBackendService.Setup(service => service.CalculateApprovedSubmission(It.IsAny<string>()))
                       .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _underTest.ProcessAsync(serviceBusMessage));

        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedErrorMessagePart)),
			It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
