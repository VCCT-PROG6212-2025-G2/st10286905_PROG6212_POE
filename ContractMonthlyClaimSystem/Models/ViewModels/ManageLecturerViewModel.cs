namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class LecturerDetailsViewModel
    {
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public string? BankDetails { get; set; }
    }

    public class ManageLecturerViewModel
    {
        public int LecturerId { get; set; }  
        public string LecturerName { get; set; } = string.Empty;
        public List<Module> AllModules { get; set; } = [];
        public List<int> AssignedModuleIds { get; set; } = [];
        public List<AssignedLecturerModuleViewModel> AssignedModulesDetailed {  get; set; } = [];
        public LecturerDetailsViewModel LecturerDetails { get; set; } = new LecturerDetailsViewModel();
    }
}
