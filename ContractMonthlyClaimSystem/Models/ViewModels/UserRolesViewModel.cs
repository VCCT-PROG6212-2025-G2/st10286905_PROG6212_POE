namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class UserRolesViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
