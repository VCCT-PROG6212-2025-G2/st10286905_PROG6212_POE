namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ManageLecturerModulesViewModel
    {
        public int LecturerId { get; set; }  
        public string LecturerName { get; set; } = string.Empty;
        public List<Module> AllModules { get; set; } = new();
        public List<int> AssignedModuleIds { get; set; } = new();
    }
}
