using Microsoft.AspNetCore.Mvc.Rendering;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class AutoReviewRulesIndexViewModel
    {
        public List<AutoReviewRuleViewModel> Rules { get; set; } = [];
        public AutoReviewRule NewRule { get; set; } = new();
    }
}
