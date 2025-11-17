// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/6911f8f7-a06c-800b-a7aa-9d3ef0330f8c

using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ManageAccountViewModel
    {
        [Display(Name = "UserName")]
        public string UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Please enter your current password to confirm changes.")]
        [Display(Name = "Current Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.EmailAddress)]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
