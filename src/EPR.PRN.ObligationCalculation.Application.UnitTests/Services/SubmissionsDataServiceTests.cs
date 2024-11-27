#nullable disable
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class SubmissionsDataServiceTests
{
    private Mock<ILogger<SubmissionsDataService>> _loggerMock;
    private Mock<IOptions<CommonDataApiConfig>> _configMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private SubmissionsDataService _service;

    private readonly string _lastSuccessfulRunDate = "2023-09-01";

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<SubmissionsDataService>>();
        _configMock = new Mock<IOptions<CommonDataApiConfig>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var config = new CommonDataApiConfig
        {
            BaseUrl = "https://api.example.com/",
            SubmissionsEndPoint = "submissions"
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_configMock.Object.Value.BaseUrl)
        };

        _service = new SubmissionsDataService(_loggerMock.Object, _httpClient, _configMock.Object);
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnValidData_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var expectedLogMessage = $"Get Approved Submissions Data from {_lastSuccessfulRunDate}";
        var organisationId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "[{ \"OrganisationId\": \"" + organisationId + "\" }]")
            });

        // Act
        var result = await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(organisationId, result[0].OrganisationId);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedLogMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnEmptyList_WhenApiResponseIsEmptyString()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent,
                Content = new StringContent(string.Empty)
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
        var expectedLogMessage = $"Error while getting submissions data";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert handled by ExpectedException
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedLogMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }
}