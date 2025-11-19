using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class CreateClaimViewModel
    {
        [Required]
        [Display(Name = "Module")]
        public int ModuleId { get; set; }
        public IEnumerable<Module>? Modules { get; set; }

        [Required]
        [Range(0.5, 1000)]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Display(Name = "Hourly Rate")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Comment (optional)")]
        public string? LecturerComment { get; set; }

        [Display(Name = "Upload Supporting Documents")]
        public List<IFormFile>? Files { get; set; }
    }
}
