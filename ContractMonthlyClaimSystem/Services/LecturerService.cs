using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class LecturerService
    {
        private readonly ApplicationDbContext _context;

        public LecturerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Module>> GetModulesAsync(string lecturerId) =>
            await (from lm in _context.LecturerModules
                   where lm.LecturerUserId == lecturerId
                   select lm.Module).ToListAsync();

        public async Task AddModuleAsync(string lecturerId, int moduleId)
        {
            var lecturerModule = new LecturerModule { LecturerUserId = lecturerId, ModuleId = moduleId };
            if (!await _context.LecturerModules.ContainsAsync(lecturerModule))
            {
                await _context.LecturerModules.AddAsync(lecturerModule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveModuleAsync(string lecturerId, int moduleId)
        {
            var lecturerModule = await (from lm in _context.LecturerModules
                                        where lm.LecturerUserId == lecturerId && lm.ModuleId == moduleId
                                        select lm).FirstOrDefaultAsync();
            if (lecturerModule != null)
            {
                _context.LecturerModules.Remove(lecturerModule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ContractClaim>> GetContractClaimsAsync(string lecturerId) =>
            await (from c in _context.ContractClaims
                   where c.LecturerUserId == lecturerId
                   select c).ToListAsync();

        public async Task CreateContractClaimAsync(string lecturerId, int moduleId, decimal hoursWorked, decimal hourlyRate)
        {
            if (!await _context.LecturerModules.ContainsAsync(new LecturerModule { LecturerUserId = lecturerId, ModuleId = moduleId }))
                throw new InvalidOperationException($"Lecturer does not teach given module.\n"
                    + "LecturerId = {lecturerId}\n"
                    + "ModuleId = {moduleId}");

            var contractClaim = new ContractClaim
            {
                LecturerUserId = lecturerId,
                ModuleId = moduleId,
                HoursWorked = hoursWorked,
                HourlyRate = hourlyRate
            };
            await _context.ContractClaims.AddAsync(contractClaim);
        }
    }
}
