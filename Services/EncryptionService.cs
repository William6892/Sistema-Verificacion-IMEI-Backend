// Services/EncryptionService.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Sistema_de_Verificación_IMEI.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            try
            {
                // Obtener claves de configuración
                var keyBase64 = configuration["Encryption:Key"]
                    ?? throw new InvalidOperationException("Encryption:Key no configurado");

                var ivBase64 = configuration["Encryption:IV"]
                    ?? throw new InvalidOperationException("Encryption:IV no configurado");

                // Convertir de Base64
                _key = Convert.FromBase64String(keyBase64);
                _iv = Convert.FromBase64String(ivBase64);

                // Validar tamaños
                if (_key.Length != 32)
                    throw new ArgumentException($"La clave debe ser de 32 bytes (256 bits). Tiene: {_key.Length} bytes");

                if (_iv.Length != 16)
                    throw new ArgumentException($"El IV debe ser de 16 bytes (128 bits). Tiene: {_iv.Length} bytes");
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Error en formato Base64 de las claves de encriptación", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al inicializar servicio de encriptación", ex);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Error criptográfico al cifrar", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al cifrar texto", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            // Verificar si es texto encriptado (formato Base64 válido)
            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                if (buffer.Length == 0)
                    return cipherText;
            }
            catch
            {
                // Si no es Base64 válido, probablemente no está encriptado
                return cipherText;
            }

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Texto cifrado no es Base64 válido", ex);
            }
            catch (CryptographicException ex)
            {
                // Esto puede ocurrir si las claves son incorrectas o el texto fue alterado
                throw new InvalidOperationException("Error al descifrar. Verifique las claves de encriptación.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al descifrar texto", ex);
            }
        }

        public string GenerateHash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}