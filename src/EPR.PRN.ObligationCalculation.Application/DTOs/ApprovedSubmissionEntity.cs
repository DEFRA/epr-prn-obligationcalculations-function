namespace EPR.PRN.ObligationCalculation.Application.DTOs
{
    public class ApprovedSubmissionEntity
    {
        public Guid SubmissionId { get; set; }
        public string? SubmissionPeriod { get; set; }
        public string? PackagingMaterial { get; set; }
        public double PackagingMaterialWeight { get; set; }
        public int OrganisationId { get; set; }
    }
}