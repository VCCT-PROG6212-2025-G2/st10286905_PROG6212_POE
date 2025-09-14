namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerClaimDetailsViewModel
    {
        public int Id { get; set; }
        public string ModuleName { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? LecturerComment { get; set; }
        public ClaimDecision ProgramCoordinatorDecision { get; set; }
        public string? ProgramCoordinatorComment { get; set; }
        public ClaimDecision AcademicManagerDecision { get; set; }
        public string? AcademicManagerComment { get; set; }
        public ClaimStatus ClaimStatus { get; set; }

        public List<UploadedFile> Files { get; set; } = [];
    }
}
