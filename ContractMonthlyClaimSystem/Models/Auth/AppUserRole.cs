using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models.Auth
{
    [Table("UserRoles")]
    [PrimaryKey(nameof(UserId), nameof(RoleId))]
    public class AppUserRole
    {
        public int UserId { get; set; }
        public AppUser User { get; set; }

        public int RoleId { get; set; }
        public AppRole Role { get; set; }
    }
}
