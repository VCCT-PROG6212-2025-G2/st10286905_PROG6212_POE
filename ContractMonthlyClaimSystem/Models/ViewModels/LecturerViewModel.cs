namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Module> Modules { get; set; } = [];
        public string? ContactNumber { get; set; }
    }
}
