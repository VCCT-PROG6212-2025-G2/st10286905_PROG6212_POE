using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models.Auth
{
    [Table("Users")]
    public class AppUser
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public ICollection<AppUserRole> UserRoles { get; set; } = [];
    }
}
