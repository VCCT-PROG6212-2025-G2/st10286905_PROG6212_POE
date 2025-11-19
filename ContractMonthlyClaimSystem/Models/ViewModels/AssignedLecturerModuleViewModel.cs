namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class AssignedLecturerModuleViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}
