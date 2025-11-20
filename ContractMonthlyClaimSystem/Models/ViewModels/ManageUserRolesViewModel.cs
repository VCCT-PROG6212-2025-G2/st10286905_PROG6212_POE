using ContractMonthlyClaimSystem.Models.Auth;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ManageUserRolesViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<RoleSelectionViewModel> Roles { get; set; } = [];
    }
}
