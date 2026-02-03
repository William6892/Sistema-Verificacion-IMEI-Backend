using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sistema_de_Verificación_IMEI.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
        {
            _logger = logger;

            try
            {
                // Obtener claves de configuración
                var keyBase64 = configuration["Encryption:Key"];
                var ivBase64 = configuration["Encryption:IV"];

                _logger.LogInformation($"Configuración - KeyBase64: {keyBase64?.Substring(0, Math.Min(20, keyBase64?.Length ?? 0))}...");
                _logger.LogInformation($"Configuración - IVBase64: {ivBase64?.Substring(0, Math.Min(20, ivBase64?.Length ?? 0))}...");

                // Si no hay configuración, usar claves por defecto
                if (string.IsNullOrEmpty(keyBase64) || string.IsNullOrEmpty(ivBase64))
                {
                    _logger.LogWarning("Usando claves de encriptación por defecto");

                    // Claves por defecto que SABEMOS funcionan
                    keyBase64 = "KzNvM2UzYTM1MzYzNzM4Mzk0MDQxNDI0MzQ0NDU=";
                    ivBase64 = "LzB6MXoxejF6MXoxejF6MQ==";
                }

                // Convertir de Base64
                _key = Convert.FromBase64String(keyBase64);
                _iv = Convert.FromBase64String(ivBase64);

                // Log para debug
                _logger.LogInformation($"Clave decodificada: {_key.Length} bytes, IV decodificado: {_iv.Length} bytes");

                // Validar tamaños (pero si no son correctos, ajustar)
                if (_key.Length != 32)
                {
                    _logger.LogWarning($"La clave tiene {_key.Length} bytes (necesita 32). Ajustando...");

                    // Ajustar a 32 bytes
                    var adjustedKey = new byte[32];
                    Buffer.BlockCopy(_key, 0, adjustedKey, 0, Math.Min(_key.Length, 32));
                    _key = adjustedKey;
                }

                if (_iv.Length != 16)
                {
                    _logger.LogWarning($"El IV tiene {_iv.Length} bytes (necesita 16). Ajustando...");

                    // Ajustar a 16 bytes
                    var adjustedIv = new byte[16];
                    Buffer.BlockCopy(_iv, 0, adjustedIv, 0, Math.Min(_iv.Length, 16));
                    _iv = adjustedIv;
                }

                _logger.LogInformation("✅ Servicio de encriptación inicializado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en encriptación. Usando claves de emergencia.");

                // Claves de emergencia
                _key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
                _iv = Encoding.UTF8.GetBytes("0123456789ABCDEF");
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

                var result = Convert.ToBase64String(ms.ToArray());
                _logger.LogDebug($"Texto encriptado: {plainText} -> {result.Substring(0, Math.Min(20, result.Length))}...");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al encriptar texto");
                throw new InvalidOperationException("Error al encriptar texto", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                // Verificar si es Base64 válido
                var buffer = Convert.FromBase64String(cipherText);
                if (buffer.Length == 0)
                    return cipherText;

                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                var result = sr.ReadToEnd();
                _logger.LogDebug($"Texto desencriptado: {cipherText.Substring(0, Math.Min(20, cipherText.Length))}... -> {result}");
                return result;
            }
            catch (FormatException)
            {
                // No es Base64 válido, devolver tal cual
                return cipherText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desencriptar texto");
                return cipherText; // En caso de error, devolver el texto original
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