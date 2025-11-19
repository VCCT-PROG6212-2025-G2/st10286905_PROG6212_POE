using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models
{
    public enum AutoReviewComparisonOp
    {
        EQUAL,
        NOT_EQUAL,
        LESS_THAN,
        LESS_THAN_OR_EQUAL,
        GREATER_THAN,
        GREATER_THAN_OR_EQUAL
    }

    public enum AutoReviewComparisonVar
    {
        HOURS_WORKED,
        HOURLY_RATE,
        PAYMENT_TOTAL
    }

    [Table("AutoReviewRules")]
    public class AutoReviewRule
    {
        public int Id { get; set; }
        public int ReviewerId { get; set; }
        public int Priority { get; set; }
        public ClaimDecision AutoDecision { get; set; }
        public AutoReviewComparisonOp ComparisonOp { get; set; }
        public AutoReviewComparisonVar ComparisonVar { get; set; }
        public decimal ComparisonValue { get; set; }
        public string? AutoComment { get; set; }
    }
}
