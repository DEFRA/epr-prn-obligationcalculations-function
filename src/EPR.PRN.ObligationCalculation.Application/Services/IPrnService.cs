namespace EPR.PRN.ObligationCalculation.Application.Services;

public interface IPrnService
{
    Task ProcessApprovedSubmission(string submissions);
}