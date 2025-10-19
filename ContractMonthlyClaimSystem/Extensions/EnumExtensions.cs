// AI Disclosure: ChatGPT assisted creating this. Link: https://chatgpt.com/share/68c7f73a-4588-800b-a812-e5ef790cd5b1

using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Extensions
{
    public static class EnumExtensions
    {
        public static string ToBgClass(this ClaimDecision decision) =>
            decision switch
            {
                ClaimDecision.PENDING => "bg-info",
                ClaimDecision.REJECTED => "bg-danger",
                ClaimDecision.VERIFIED => "bg-success",
                ClaimDecision.APPROVED => "bg-success",
                _ => "bg-info",
            };

        public static string ToBgClass(this ClaimStatus status) =>
            status switch
            {
                ClaimStatus.PENDING => "bg-info",
                ClaimStatus.PENDING_CONFIRM => "bg-info",
                ClaimStatus.ACCEPTED => "bg-success",
                ClaimStatus.REJECTED => "bg-danger",
                _ => "bg-info",
            };
    }
}
