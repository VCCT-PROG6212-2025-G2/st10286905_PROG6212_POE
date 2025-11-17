namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ManageUserRolesViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public List<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
    }
}
