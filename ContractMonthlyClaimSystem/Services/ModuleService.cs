using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class ModuleService(ApplicationDbContext context) : IModuleService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<Module>> GetModulesAsync() => await _context.Modules.ToListAsync();

        public async Task<List<Module>> GetModulesForLecturerAsync(string lecturerId) =>
            await (
                from lm in _context.LecturerModules
                where lm.LecturerUserId == lecturerId
                select lm.Module
            ).ToListAsync();

        public async Task AddModuleAsync(Module module)
        {
            if (string.IsNullOrWhiteSpace(module.Name) || string.IsNullOrWhiteSpace(module.Code))
                return;

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();
        }

        public async Task AddModuleAsync(string name, string code) =>
            await AddModuleAsync(new Module { Name = name, Code = code });

        public async Task RemoveModuleAsync(int moduleId)
        {
            var module = await _context.Modules.FindAsync(moduleId);
            if (module != null)
            {
                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddLecturerModuleAsync(string lecturerId, int moduleId)
        {
            var lecturerModule = new LecturerModule
            {
                LecturerUserId = lecturerId,
                ModuleId = moduleId,
            };
            if (!await _context.LecturerModules.ContainsAsync(lecturerModule))
            {
                _context.LecturerModules.Add(lecturerModule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveLecturerModuleAsync(string lecturerId, int moduleId)
        {
            var lecturerModule = await _context.LecturerModules.FirstOrDefaultAsync(lm =>
                lm.LecturerUserId == lecturerId && lm.ModuleId == moduleId
            );
            if (lecturerModule != null)
            {
                _context.LecturerModules.Remove(lecturerModule);
                await _context.SaveChangesAsync();
            }
        }
    }
}
