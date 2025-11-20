namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IFileEncryptionService
    {
        Task EncryptToFileAsync(Stream input, string outputPath);
        Task DecryptToStreamAsync(string inputPath, Stream output);
        Stream OpenDecryptedRead(string inputPath);
    }
}
