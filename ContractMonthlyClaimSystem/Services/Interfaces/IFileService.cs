using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IFileService
    {
        public Task<UploadedFile?> UploadFileAsync(Stream fileStream, string fileName, long fileSize);
        public Task<UploadedFile?> UploadFileAsync(IFormFile file);
        public Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(int fileId);
        public Task<bool> DeleteFileAsync(int fileId);
    }
}
