namespace ContractMonthlyClaimSystem.Services.Interfaces
{
    public interface IPasswordHasher
    {
        (string Hash, string Salt) HashPassword(string password);
        bool Verify(string password, string storedHash, string storedSalt);
    }
}
