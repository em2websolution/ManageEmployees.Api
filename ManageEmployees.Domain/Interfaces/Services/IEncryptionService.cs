namespace ManageEmployees.Domain.Interfaces.Services
{
    public interface IEncryptionService
    {
        string Decrypt(string data);
    }
}
