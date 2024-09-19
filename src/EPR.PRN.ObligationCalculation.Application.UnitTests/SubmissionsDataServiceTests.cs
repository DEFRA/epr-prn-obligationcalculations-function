#nullable disable

using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests;

[ExcludeFromCodeCoverage]
[TestClass]
public class SubmissionsDataServiceTests
{
    string approvedAfterDateString;
    List<ApprovedSubmissionEntity> approvedSubmissionEntities;
    List<ApprovedSubmissionEntity> emptyList;
    Mock<ISubmissionsDataService> submissionsDataService;

    [TestInitialize]
    public void TestInitialize()
    {
        approvedAfterDateString = "2024-01-01";
        approvedSubmissionEntities =
        [
            new() {
                SubmissionId = Guid.Empty,
                PackagingMaterial = "PC",
                PackagingMaterialWeight = 100,
                SubmissionPeriod = "2024 P1",
                OrganisationId = 1234
            },
            new() {
                SubmissionId = Guid.Empty,
                PackagingMaterial = "PL",
                PackagingMaterialWeight = 200,
                SubmissionPeriod = "2024 P2",
                OrganisationId = 1234
            }
        ];
        emptyList = new List<ApprovedSubmissionEntity>();

        submissionsDataService = new Mock<ISubmissionsDataService>();
        submissionsDataService.Setup(x => x.GetApprovedSubmissionsData(approvedAfterDateString)).ReturnsAsync(approvedSubmissionEntities);
    }

    [TestMethod]
    public async Task GetApprovedSubmissionsData_WhenCalled_ShouldReturnApprovedSubmissionsData()
    {
        // Arrange
        var service = submissionsDataService.Object;

        // Act
        var result = await service.GetApprovedSubmissionsData(approvedAfterDateString);

        // Assert
        submissionsDataService.Verify(x => x.GetApprovedSubmissionsData(approvedAfterDateString), Times.Once);
        Assert.AreEqual(approvedSubmissionEntities, result);
    }

    [TestMethod]
    public async Task GetApprovedSubmissionsData_WhenExceptionThrown_ShouldThrowException()
    {
        // Arrange
        submissionsDataService.Setup(x => x.GetApprovedSubmissionsData(approvedAfterDateString)).ThrowsAsync(new Exception("Error while getting submissions data"));

        var service = submissionsDataService.Object;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => service.GetApprovedSubmissionsData(approvedAfterDateString));
    }

    [TestMethod]
    public async Task GetApprovedSubmissionsData_WhenNoDataFound_ShouldReturnEmptyList()
    {
        // Arrange
        submissionsDataService.Setup(x => x.GetApprovedSubmissionsData(approvedAfterDateString)).ReturnsAsync(emptyList);

        var service = submissionsDataService.Object;

        // Act
        var result = await service.GetApprovedSubmissionsData(approvedAfterDateString);

        // Assert
        submissionsDataService.Verify(x => x.GetApprovedSubmissionsData(approvedAfterDateString), Times.Once);
        Assert.AreEqual(emptyList, result);
    }
}
