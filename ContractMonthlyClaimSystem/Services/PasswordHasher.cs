// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/690a140c-df58-800b-8dda-d684e3acea06

using System.Security.Cryptography;
using ContractMonthlyClaimSystem.Services.Interfaces;

namespace ContractMonthlyClaimSystem.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                100_000,
                HashAlgorithmName.SHA256,
                32
            );

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public bool Verify(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                100_000,
                HashAlgorithmName.SHA256,
                32
            );

            return CryptographicOperations.FixedTimeEquals(
                hashBytes,
                Convert.FromBase64String(storedHash)
            );
        }
    }
}
