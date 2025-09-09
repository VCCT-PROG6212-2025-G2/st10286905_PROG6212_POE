using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Models
{
    [PrimaryKey(nameof(LecturerUserId), nameof(ModuleId))]
    public class LecturerModule
    {
        public string LecturerUserId { get; set; }
        public virtual AppUser LecturerUser { get; set; }

        public int ModuleId { get; set; }
        public virtual Module Module { get; set; }
    }
}
