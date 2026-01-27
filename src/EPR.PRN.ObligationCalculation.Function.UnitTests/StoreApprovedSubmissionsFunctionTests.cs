using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Function.Services;
using EPR.PRN.ObligationCalculation.Function.UnitTests.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests;

[TestClass]
public class StoreApprovedSubmissionsFunctionTests
{
    private Mock<ILogger<StoreApprovedSubmissionsFunction>> _mockLogger = null!;
    private Mock<IEprCommonDataApiService> _mockEprCommonDataApiService = null!;
    private Mock<IEprPrnCommonBackendService> _mockEprPrnCommonBackendService = null!;
    private Mock<IOptions<ApplicationConfig>> _mockConfig = null!;
    private StoreApprovedSubmissionsFunction _underTest = null!;
    private TimerInfo _timerInfo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<StoreApprovedSubmissionsFunction>>();
        _timerInfo = new TimerInfo();
        _mockEprCommonDataApiService = new Mock<IEprCommonDataApiService>();
        _mockEprPrnCommonBackendService = new Mock<IEprPrnCommonBackendService>();

        _mockConfig = new Mock<IOptions<ApplicationConfig>>();
        var config = new ApplicationConfig
        {
            DefaultRunDate = "2024-01-01",
            FunctionIsEnabled = true,
            LogPrefix = "TEST"
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

        _underTest = new StoreApprovedSubmissionsFunction(
            _mockLogger.Object,
            _mockEprCommonDataApiService.Object,
            _mockEprPrnCommonBackendService.Object,
            _mockConfig.Object);
    }

    // -------------------------- PUBLISH --------------------------

    [TestMethod]
    public async Task PublishAsync_FunctionDisabled_ShouldLogAndExit()
    {
        var config = new ApplicationConfig { FunctionIsEnabled = false, LogPrefix = "TEST" };
        _mockConfig.Setup(x => x.Value).Returns(config);

        // Act
        await _underTest.PublishAsync(_timerInfo);

        // Assert - logs the disabled message
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Exiting function")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // No further calls to backend service
        _mockEprPrnCommonBackendService.Verify(s => s.CalculateApprovedSubmission(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PublishAsync_NoSubmissions_ShouldLogAndExit()
    {
        _mockEprCommonDataApiService.Setup(s => s.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ReturnsAsync(new List<ApprovedSubmissionEntity>());

        // Act
        await _underTest.PublishAsync(_timerInfo);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("No submissions received")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _mockEprPrnCommonBackendService.Verify(s => s.CalculateApprovedSubmission(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PublishAsync_WithSubmissions_ShouldCallCalculateApprovedSubmission()
    {
        // Act
        await _underTest.PublishAsync(_timerInfo);

        // Assert
        _mockEprPrnCommonBackendService.Verify(
            s => s.CalculateApprovedSubmission(It.Is<string>(body => body.Contains("Plastic"))),
            Times.Once);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("successfully calculated")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task PublishAsync_WhenExceptionOccurs_ShouldLogErrorAndThrow()
    {
        _mockEprCommonDataApiService.Setup(s => s.GetApprovedSubmissionsData(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test Exception"));

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => _underTest.PublishAsync(_timerInfo));

        Assert.AreEqual("Test Exception", ex.Message);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Ended with error while storing approved submission")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
