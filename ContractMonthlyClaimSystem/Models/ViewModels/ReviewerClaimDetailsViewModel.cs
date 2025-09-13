namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ReviewerClaimDetailsViewModel
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public string ModuleName { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? LecturerComment { get; set; }
        public string? ProgramCoordinatorName { get; set; }
        public bool? ProgramCoordinatorAccepted { get; set; }
        public string? ProgramCoordinatorComment { get; set; }
        public string? AcademicManagerName { get; set; }
        public bool? AcademicManagerAccepted { get; set; }
        public string? AcademicManagerComment { get; set; }
        public ClaimStatus ClaimStatus { get; set; }

        public List<UploadedFile> Files { get; set; } = [];
    }
}
