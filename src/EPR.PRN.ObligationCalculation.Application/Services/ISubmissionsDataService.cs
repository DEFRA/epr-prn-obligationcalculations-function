using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public interface ISubmissionsDataService
{
    Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string lastSuccessfulRunDate);
}