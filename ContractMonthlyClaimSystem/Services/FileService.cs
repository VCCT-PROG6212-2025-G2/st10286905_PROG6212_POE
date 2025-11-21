using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Services
{
    public class FileService(
        AppDbContext context,
        IWebHostEnvironment env,
        IFileEncryptionService encryptionService
    ) : IFileService
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly IFileEncryptionService _encryptionService = encryptionService;

        public async Task<UploadedFile?> UploadFileAsync(
            Stream fileStream,
            string fileName,
            long fileSize
        )
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileSize <= 0)
                return null;

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // Ensure file name is unique and sanitized/secure
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);

            // Encrypt and write file to disk
            await _encryptionService.EncryptToFileAsync(fileStream, filePath);

            // Ensure file exists and is valid
            await using (var tmp = File.Open(filePath, FileMode.Open))
            {
                if (fileSize > 16 && tmp.Length <= 16)
                { // Original file is more than 16 bytes but encrypted file is <= minimum encrypted size, something went wrong
                    tmp.Close(); // Close invalid file
                    File.Delete(filePath); // Delete invalid file
                    return null; // Return early
                }
            }

            // Create UploadedFile entry and add it to database
            var uploadedFile = new UploadedFile
            {
                FileName = fileName,
                FilePath = $"uploads/{uniqueFileName}",
                FileSize = fileSize,
                UploadedOn = DateTime.Now,
            };
            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            // Finally, return the UploadedFile entry.
            return uploadedFile;
        }

        public async Task<UploadedFile?> UploadFileAsync(IFormFile file)
        {
            using var inputStream = file.OpenReadStream();
            return await UploadFileAsync(inputStream, file.FileName, file.Length);
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(
            int fileId
        )
        {
            var file = await _context.UploadedFiles.FindAsync(fileId);
            if (file == null)
                return null; // Can't find file ..

            // Figure out file path
            var relativePath = file.FilePath.TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );
            var filePath = Path.Combine(_env.WebRootPath, relativePath);
            if (!File.Exists(filePath))
                return null; // File doesn't exist ..

            // Open decrypting read stream
            var decryptingStream = _encryptionService.OpenDecryptedRead(filePath);

            // Try get content type, setting fallback if it fails.
            // AI Disclosure: ChatGPT assisted me with this. Link: https://chatgpt.com/s/t_691f014aee748191aa527bb938fa1fc5
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(file.FileName, out string? contentType))
                contentType = "application/octet-stream";

            return (decryptingStream, contentType, file.FileName);
        }

        public async Task<bool> DeleteFileAsync(int fileId)
        {
            var file = await _context.UploadedFiles.FindAsync(fileId);
            if (file == null)
                return false; // Can't find file ..

            // Remove file from db
            _context.UploadedFiles.Remove(file);
            await _context.SaveChangesAsync();

            // Figure out file path
            var relativePath = file.FilePath.TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );
            var filePath = Path.Combine(_env.WebRootPath, relativePath);
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return true; // File doesn't exist ..

            // Delete file
            try
            {
                File.Delete(filePath);
            }
            catch { // Delete failed
                // Add file back to db
                _context.UploadedFiles.Add(file);
                await _context.SaveChangesAsync();
                return false;
            }

            if (File.Exists(filePath) || Directory.Exists(filePath))
            {// Delete failed
                // Add file back to db
                _context.UploadedFiles.Add(file);
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }
    }
}
