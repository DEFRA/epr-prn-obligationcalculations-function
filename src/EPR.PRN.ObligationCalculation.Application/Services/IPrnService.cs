namespace EPR.PRN.ObligationCalculation.Application.Services;

public interface IPrnService
{
    Task<string> GetLastSuccessfulRunDate();
    Task UpdateLastSuccessfulRunDate(DateTime currentDateTime);
    Task ProcessApprovedSubmission(string submissions);
}