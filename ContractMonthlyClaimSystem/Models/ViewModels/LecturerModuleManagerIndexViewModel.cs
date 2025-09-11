namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerModuleManagerIndexViewModel
    {
        public List<LecturerViewModel> Lecturers { get; set; } = new();
        public List<Module> Modules { get; set; } = new();
        public Module NewModule { get; set; } = new();
    }
}
