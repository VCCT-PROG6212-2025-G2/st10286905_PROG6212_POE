using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class LecturerClaimService(
        AppDbContext context,
        IWebHostEnvironment env,
        IModuleService moduleService,
        IFileEncryptionService encryptionService
    ) : ILecturerClaimService
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly IModuleService _moduleService = moduleService;
        private readonly IFileEncryptionService _encryptionService = encryptionService;

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
                HourlyRate = model.HourlyRate,
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

            // AI Disclaimer: I made use of ChatGPT to assist with upload functionality. Link: https://chatgpt.com/share/68cac0b4-34b8-800b-b47c-a65ef55ad8e5
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsDir);

                    // Ensure file name is unique and sanitized/secure
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using var inputStream = file.OpenReadStream();
                    await _encryptionService.EncryptToFileAsync(inputStream, filePath);

                    var uploadedFile = new UploadedFile
                    {
                        FileName = file.FileName,
                        FilePath = $"uploads/{uniqueFileName}",
                        FileSize = file.Length,
                        UploadedOn = DateTime.Now,
                    };
                    _context.UploadedFiles.Add(uploadedFile);
                    await _context.SaveChangesAsync();

                    var claimDoc = new ContractClaimDocument
                    {
                        ContractClaimId = claim.Id,
                        UploadedFileId = uploadedFile.Id,
                    };
                    _context.ContractClaimsDocuments.Add(claimDoc);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<(
            string FileName,
            MemoryStream FileStream,
            string ContentType
        )?> GetFileAsync(int fileId, int lecturerId)
        {
            var file = await (
                from d in _context.ContractClaimsDocuments
                join c in _context.ContractClaims on d.ContractClaimId equals c.Id
                where c.LecturerUserId == lecturerId && d.UploadedFileId == fileId
                select d.UploadedFile
            ).FirstOrDefaultAsync();
            if (file == null)
                return null;

            var filePath = Path.Combine(_env.WebRootPath, file.FilePath);
            if (!System.IO.File.Exists(filePath))
                return null;

            var output = new MemoryStream();
            await _encryptionService.DecryptToStreamAsync(filePath, output);
            output.Position = 0;

            var contentType = "application/octet-stream";

            return (file.FileName, output, contentType);
        }
    }
}
