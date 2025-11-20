using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class LecturerClaimService(
        AppDbContext context,
        IModuleService moduleService,
        IFileService fileService
    ) : ILecturerClaimService
    {
        private readonly AppDbContext _context = context;
        private readonly IModuleService _moduleService = moduleService;
        private readonly IFileService _fileService = fileService;

        public async Task<List<ContractClaim>> GetClaimsForLecturerAsync(int lecturerId) =>
            await _context
                .ContractClaims.Include(c => c.Module)
                .Where(c => c.LecturerUserId == lecturerId)
                .ToListAsync();

        public async Task<ContractClaim?> GetClaimAsync(int claimId, int lecturerId) =>
            await _context
                .ContractClaims.Include(c => c.Module)
                .FirstOrDefaultAsync(c => c.Id == claimId && c.LecturerUserId == lecturerId);

        public async Task<List<UploadedFile>?> GetClaimFilesAsync(ContractClaim claim) =>
            await (
                from d in _context.ContractClaimsDocuments
                where d.ContractClaimId == claim.Id
                select d.UploadedFile
            ).ToListAsync();

        public async Task<List<Module>> GetModulesForLecturerAsync(int lecturerId) =>
            await _moduleService.GetModulesForLecturerAsync(lecturerId);

        public async Task<decimal?> GetLecturerHourlyRateAsync(int lecturerId, int moduleId) =>
            (
                await _context.LecturerModules.FirstOrDefaultAsync(lm =>
                    lm.LecturerUserId == lecturerId && lm.ModuleId == moduleId
                )
            )?.HourlyRate;

        public async Task<ContractClaim> CreateClaimAsync(
            int lecturerId,
            CreateClaimViewModel model
        )
        {
            var claim = new ContractClaim
            {
                LecturerUserId = lecturerId,
                ModuleId = model.ModuleId,
                HoursWorked = model.HoursWorked,
                HourlyRate = await GetLecturerHourlyRateAsync(lecturerId, model.ModuleId) ?? 0m,
                LecturerComment = model.LecturerComment,
            };

            _context.ContractClaims.Add(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task AddFilesToClaimAsync(ContractClaim claim, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
                return;

            foreach (var file in files)
            {
                var uploadedFile = await _fileService.UploadFileAsync(file);
                if (uploadedFile == null)
                    continue;

                var claimDoc = new ContractClaimDocument
                {
                    ContractClaimId = claim.Id,
                    UploadedFileId = uploadedFile.Id,
                };
                _context.ContractClaimsDocuments.Add(claimDoc);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(
            int fileId,
            int lecturerId
        )
        {
            var claimDoc = await _context.ContractClaimsDocuments.FirstOrDefaultAsync(d =>
                d.UploadedFileId == fileId
            );
            if (claimDoc == null)
                return null;

            var claim = await _context.ContractClaims.FirstOrDefaultAsync(c =>
                c.Id == claimDoc.ContractClaimId
            );
            if (claim == null || claim.LecturerUserId != lecturerId)
                return null;

            return await _fileService.GetFileAsync(fileId);
        }
    }
}
