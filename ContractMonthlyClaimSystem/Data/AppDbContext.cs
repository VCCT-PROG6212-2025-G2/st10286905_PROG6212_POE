using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<AppRole> Roles { get; set; }
        public DbSet<AppUserRole> UserRoles { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<LecturerModule> LecturerModules { get; set; }
        public DbSet<ContractClaim> ContractClaims { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<ContractClaimDocument> ContractClaimsDocuments { get; set; }
        public DbSet<AutoReviewRule> AutoReviewRules { get; set; }
    }
}
