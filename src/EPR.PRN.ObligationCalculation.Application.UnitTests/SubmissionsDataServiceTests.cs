#nullable disable

using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests;

[TestClass]
public class SubmissionsDataServiceTests
{
    private Mock<ILogger<SubmissionsDataService>> _loggerMock;
    private Mock<IOptions<SubmissionsApiConfig>> _configMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private SubmissionsDataService _service;

    private readonly string _lastSuccessfulRunDate = "2023-09-01";
    private string _submissionsEndpoint = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<SubmissionsDataService>>();
        _configMock = new Mock<IOptions<SubmissionsApiConfig>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var config = new SubmissionsApiConfig
        {
            BaseUrl = "https://api.example.com/",
            EndPoint = "submissions"
        };
        _configMock.Setup(c => c.Value).Returns(config);
        _submissionsEndpoint = $"{_configMock.Object.Value.BaseUrl}{_configMock.Object.Value.EndPoint}{_lastSuccessfulRunDate}";
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new SubmissionsDataService(_loggerMock.Object, _httpClient, _configMock.Object);
    }

    [TestMethod]
    public async Task GetApprovedSubmissionsData_ShouldLogCorrectMessage()
    {
        // Arrange
        var logPrefix = ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix;
        var expectedLogMessage = $"{logPrefix} >>>>>>> Get Approved Submissions Data from {_lastSuccessfulRunDate} <<<<<<<<";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    """
                    [
                        {
                            "OrganisationId": "123"
                        }
                    ]
                    """)
            });

        // Act
        await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedLogMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnValidData_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var approvedSubmissions = new List<ApprovedSubmissionEntity> { new() { OrganisationId = 123 } };
        var jsonResponse = JsonConvert.SerializeObject(approvedSubmissions);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == _submissionsEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(123, result[0].OrganisationId);
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnEmptyList_WhenApiResponseIsEmptyString()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == _submissionsEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty)
            });

        // Act
        var result = await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No submissions data found")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnEmptyList_WhenApiResponseIsEmptyArray()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == _submissionsEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        // Act
        var result = await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public async Task GetSubmissions_ShouldThrowException_WhenHttpClientThrowsException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == _submissionsEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert handled by ExpectedException
    }
}