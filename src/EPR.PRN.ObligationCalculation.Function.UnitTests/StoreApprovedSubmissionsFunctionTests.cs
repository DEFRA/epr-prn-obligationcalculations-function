#nullable disable

using AutoFixture;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.Tests;

[TestClass()]
public class StoreApprovedSubmissionsFunctionTests
{
    private Fixture fixture;
    private Mock<ISubmissionsDataService> _submissionsDataService;
    private Mock<ILogger<StoreApprovedSubmissionsFunction>> _loggerMock;
    private Mock<IServiceBusProvider> _serviceBusProviderMock;
    private StoreApprovedSubmissionsFunction _function;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new Fixture();
        _loggerMock = new Mock<ILogger<StoreApprovedSubmissionsFunction>>();
        _submissionsDataService = new Mock<ISubmissionsDataService>();
        _serviceBusProviderMock = new Mock<IServiceBusProvider>();
        _function = new StoreApprovedSubmissionsFunction(
            _loggerMock.Object,
            _submissionsDataService.Object,
            _serviceBusProviderMock.Object);
    }

    [TestMethod()]
    public async Task RunAsync_ShouldSendApprovedSubmissionsToQueue_WhenSubmissionsAreAvailable()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var approvedSubmissionEntities = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();
        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ReturnsAsync(approvedSubmissionEntities);
        _serviceBusProviderMock
            .Setup(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities))
            .Returns(Task.CompletedTask);
        // Act
        await _function.RunAsync(timerInfo);

        // Assert
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(3));
        _serviceBusProviderMock.Verify(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities), Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_ShouldLogNoSubmissions_WhenNoSubmissionsAvailable()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var approvedSubmissions = new List<ApprovedSubmissionEntity>();
        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ReturnsAsync(approvedSubmissions);
        // Act
        await _function.RunAsync(timerInfo);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));

        _serviceBusProviderMock.Verify(x => x.SendApprovedSubmissionsToQueue(It.IsAny<List<ApprovedSubmissionEntity>>()), Times.Never);
    }



    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public async Task RunAsync_ShouldHandleException_WhenErrorOccurs()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        _submissionsDataService
            .Setup(x => x.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _function.RunAsync(timerInfo);

        // Assert
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