namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class AutoReviewRuleViewModel
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public ClaimDecision AutoDecision { get; set; }
        public AutoReviewComparisonVar ComparisonVar { get; set; }
        public AutoReviewComparisonOp ComparisonOp { get; set; }
        public decimal ComparisonValue { get; set; }
        public string? AutoComment { get; set; }
    }
}
