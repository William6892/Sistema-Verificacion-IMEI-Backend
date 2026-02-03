// Services/IEncryptionService.cs
namespace Sistema_de_Verificación_IMEI.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string GenerateHash(string input);
    }
}