using ContractMonthlyClaimSystem.Models.Auth;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models
{
    [PrimaryKey(nameof(UserId))]
    public class LecturerDetails
    {
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public virtual AppUser User { get; set; }

        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public string? BankDetails { get; set; }
    }
}
