using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models.Auth
{
    [Table("Roles")]
    public class AppRole
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<AppUserRole> UserRoles { get; set; } = [];
    }
}
