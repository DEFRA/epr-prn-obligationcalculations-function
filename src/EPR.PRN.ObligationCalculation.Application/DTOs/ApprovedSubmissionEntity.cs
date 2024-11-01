using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ApprovedSubmissionEntity
{
    public Guid OrganisationId { get; set; }
    public string? SubmissionPeriod { get; set; }
    public string? PackagingMaterial { get; set; }
    public double PackagingMaterialWeight { get; set; }
}