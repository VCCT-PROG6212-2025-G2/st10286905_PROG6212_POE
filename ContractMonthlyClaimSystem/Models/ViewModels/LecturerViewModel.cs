namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Module> Modules { get; set; } = new List<Module>();
    }
}
