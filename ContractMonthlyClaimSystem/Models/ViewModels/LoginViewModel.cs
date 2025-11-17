// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/6911f8f7-a06c-800b-a7aa-9d3ef0330f8c

using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "UserName is required.")]
        [Display(Name = "UserName")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
