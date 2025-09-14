namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ReviewerClaimRowViewModel
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public string ModuleName { get; set; }
        public decimal PaymentAmount { get; set; }
        public ClaimDecision ProgramCoordinatorDecision { get; set; }
        public ClaimDecision AcademicManagerDecision { get; set; }
        public ClaimStatus ClaimStatus { get; set; }
    }

    public class ReviewerClaimsViewModel
    {
        public List<ReviewerClaimRowViewModel> PendingClaims { get; set; }
        public List<ReviewerClaimRowViewModel> PendingConfirmClaims { get; set; }
        public List<ReviewerClaimRowViewModel> CompletedClaims { get; set; }
    }
}
