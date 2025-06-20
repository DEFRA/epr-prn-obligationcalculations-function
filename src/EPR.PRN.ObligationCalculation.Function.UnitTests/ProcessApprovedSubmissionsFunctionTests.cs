﻿using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Function.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Function.UnitTests;

[TestClass]
public class ProcessApprovedSubmissionsFunctionTests
{
    private Mock<ILogger<ProcessApprovedSubmissionsFunction>> _loggerMock = null!;
    private Mock<IPrnService> _prnServiceMock = null!;
    private ProcessApprovedSubmissionsFunction _function = null!;
    private Mock<IOptions<ApplicationConfig>> _configMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ProcessApprovedSubmissionsFunction>>();
        _prnServiceMock = new Mock<IPrnService>();

        _configMock = new Mock<IOptions<ApplicationConfig>>();
        var config = new ApplicationConfig
        {
            DefaultRunDate = "2024-01-01",
            FunctionIsEnabled = true
        };

        _configMock.Setup(c => c.Value).Returns(config);

        _function = new ProcessApprovedSubmissionsFunction(_loggerMock.Object, _prnServiceMock.Object, _configMock.Object);
    }

    [TestMethod]
    public async Task RunAsync_FunctionIsEnabled_ShouldProcessMessageSuccessfully()
    {
        var config = new ApplicationConfig
        {
            FunctionIsEnabled = true
        };

        _configMock.Setup(x => x.Value).Returns(config);

        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _function.RunAsync(serviceBusMessage);

        // Assert
        _prnServiceMock.Verify(service => service.ProcessApprovedSubmission("test-message-body"), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
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
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _function.RunAsync(serviceBusMessage);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task RunAsync_ShouldProcessMessageSuccessfully()
    {
        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");

        // Act
        await _function.RunAsync(serviceBusMessage);

        // Assert
        _prnServiceMock.Verify(service => service.ProcessApprovedSubmission("test-message-body"), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task RunAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var serviceBusMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage("test-message-body");
        var expectedErrorMessagePart = "ProcessApprovedSubmissionsFunction: Exception occurred while processing message";

		_prnServiceMock.Setup(service => service.ProcessApprovedSubmission(It.IsAny<string>()))
                       .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _function.RunAsync(serviceBusMessage));

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedErrorMessagePart)),
			It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
