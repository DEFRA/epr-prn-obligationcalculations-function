#nullable disable
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Newtonsoft.Json;
using Moq;
using Moq.Protected;
using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class PrnServiceTests
{
    private Mock<ILogger<PrnService>> _loggerMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<IOptions<CommonBackendApiConfig>> _configMock;
    private HttpClient _httpClient;
    private PrnService _prnService;
    private CommonBackendApiConfig _config;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PrnService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _configMock = new Mock<IOptions<CommonBackendApiConfig>>();

        // Setup config
        _config = new CommonBackendApiConfig
        {
            BaseUrl = "http://test-url.com/",
            LastSuccessfulRunDateEndPoint = "api/lastrun",
            PrnCalculateEndPoint = "api/calculate/{0}"
        };

        _configMock.Setup(c => c.Value).Returns(_config);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_configMock.Object.Value.BaseUrl)
        };

        _prnService = new PrnService(_loggerMock.Object, _httpClient, _configMock.Object);
    }

    [TestMethod]
    public async Task GetLastSuccessfulRunDate_ShouldReturnDate_WhenResponseIsOk()
    {
        // Arrange
        var expectedDate = "2024-01-01";
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedDate)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _prnService.GetLastSuccessfulRunDate();

        // Assert
        Assert.AreEqual(expectedDate, result);
    }

    [TestMethod]
    public async Task GetLastSuccessfulRunDate_ShouldReturnEmpty_WhenResponseIsNotOk()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _prnService.GetLastSuccessfulRunDate();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task UpdateLastSuccessfulRunDate_ShouldSendPutRequest()
    {
        // Arrange
        var currentDate = DateTime.Now;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _prnService.UpdateLastSuccessfulRunDate(currentDate);

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldLogInformation_WhenSubmissionIsEmpty()
    {
        // Arrange
        string emptySubmission = string.Empty;

        // Act
        await _prnService.ProcessApprovedSubmission(emptySubmission);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldSendPostRequest_WhenSubmissionIsNotEmpty()
    {
        // Arrange
        var submission = JsonConvert.SerializeObject(new List<ApprovedSubmissionEntity>
        {
            new () { SubmissionId = Guid.NewGuid() }
        });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        await _prnService.ProcessApprovedSubmission(submission);

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post && r.RequestUri.ToString().Contains(_config.BaseUrl)),
                ItExpr.IsAny<CancellationToken>());
    }
}