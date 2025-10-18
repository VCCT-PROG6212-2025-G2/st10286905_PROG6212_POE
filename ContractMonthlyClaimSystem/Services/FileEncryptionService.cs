// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f3f2c2-0354-800b-bd9a-666184acbc34

using ContractMonthlyClaimSystem.Services.Interfaces;
using System.Security.Cryptography;

namespace ContractMonthlyClaimSystem.Services
{
    public class FileEncryptionService : IFileEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public FileEncryptionService(IConfiguration config)
        {
            _key = Convert.FromBase64String(
                config["Encryption:Key"]
                    ?? throw new InvalidOperationException(
                        "Encryption Key missing from appsettings.json"
                    )
            );
            if (_key.Length != 32)
                throw new InvalidOperationException($"Invalid Key length: {_key.Length} bytes. Expected 32.");
            _iv = Convert.FromBase64String(
                config["Encryption:IV"]
                    ?? throw new InvalidOperationException(
                        "Encryption IV missing from appsettings.json"
                    )
            );
            if (_iv.Length != 16)
                throw new InvalidOperationException($"Invalid IV length: {_iv.Length} bytes. Expected 16.");
        }

        public async Task EncryptToFileAsync(Stream input, string outputPath)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            await using var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await input.CopyToAsync(cryptoStream);
        }

        public async Task DecryptToStreamAsync(string inputPath, Stream output) 
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            await using var fileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            await using var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await cryptoStream.CopyToAsync(output);
        }
    }
}
