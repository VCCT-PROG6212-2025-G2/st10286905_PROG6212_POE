namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ApprovedClaimRowViewModel
    {
        public int Id { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public ClaimDecision ProgramCoordinatorDecision { get; set; }
        public ClaimDecision AcademicManagerDecision { get; set; }
        public ClaimStatus ClaimStatus { get; set; }
        public string? InvoiceFileName { get; set; }
    }

    public class InvoicesIndexViewModel
    {
        public List<ApprovedClaimRowViewModel> ApprovedClaims { get; set; } = [];
    }
}
