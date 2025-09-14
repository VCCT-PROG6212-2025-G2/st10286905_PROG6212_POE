namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerClaimRowViewModel
    {
        public int Id { get; set; }
        public string ModuleName { get; set; }
        public decimal PaymentAmount { get; set; }
        public ClaimDecision ProgramCoordinatorDecision { get; set; }
        public ClaimDecision AcademicManagerDecision { get; set; }
        public ClaimStatus ClaimStatus { get; set; }
    }

    public class LecturerClaimsViewModel
    {
        public List<LecturerClaimRowViewModel> PendingClaims { get; set; } = new();
        public List<LecturerClaimRowViewModel> CompletedClaims { get; set; } = new();
    }
}
