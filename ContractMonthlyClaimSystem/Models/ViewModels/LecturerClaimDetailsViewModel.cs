namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerClaimDetailsViewModel
    {
        public int Id { get; set; }
        public string ModuleName { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string? LecturerComment { get; set; }
        public ClaimStatus ClaimStatus { get; set; }

        public List<UploadedFile> Files { get; set; } = [];
    }
}
