#nullable disable
using AutoFixture;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Tests.Services;

[TestClass]
public class ServiceBusProviderTests
{
    private Mock<ILogger<ServiceBusProvider>> _loggerMock;
    private Mock<ServiceBusClient> _serviceBusClientMock;
    private Mock<IOptions<ServiceBusConfig>> _configMock;

    private Mock<ServiceBusSender> _serviceBusSenderMock;
    private ServiceBusProvider _serviceBusProvider;
    private Fixture fixture;


    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new Fixture();
        _loggerMock = new Mock<ILogger<ServiceBusProvider>>();
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _configMock = new Mock<IOptions<ServiceBusConfig>>();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();

        var config = new ServiceBusConfig { QueueName = "test-queue" };
        _configMock.Setup(c => c.Value).Returns(config);

        _serviceBusProvider = new ServiceBusProvider(
            _loggerMock.Object,
            _serviceBusClientMock.Object,
            _configMock.Object
        );
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueue_Success()
    {
        // Arrange
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(1000, []);

        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        var approvedSubmissions = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();

        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueue(approvedSubmissions);

        // Assert
        _serviceBusSenderMock.Verify(sender => sender.CreateMessageBatchAsync(default), Times.Once);
        _serviceBusSenderMock.Verify(sender => sender.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), default), Times.Once);
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueue_MessageTooLarge_Warns()
    {
        // Arrange
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(1000, []);
        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
        messageBatch.TryAddMessage(new ServiceBusMessage());
        var approvedSubmissions = fixture.Build<ApprovedSubmissionEntity>()
            .With(x => x.OrganisationId, 123)
            .CreateMany(1000)
            .ToList();
        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueue(approvedSubmissions);


        // Assert
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }
}
