using System.Security.Cryptography;
using System.Text;
using ReShareBlazorApp.Api.Interfaces.Services;

namespace ReShareBlazorApp.Api.Services
{
    public class MessageEncryptionService : IMessageEncryptionService
    {
        private readonly byte[] _key;
        private readonly ILogger<MessageEncryptionService> _logger;

        public MessageEncryptionService(IConfiguration configuration, ILogger<MessageEncryptionService> logger)
        {
            _logger = logger;

            // Używamy dedykowanego klucza z konfiguracji lub bezpiecznego klucza wyliczonego z Jwt:Key
            var configuredKey = configuration["MessageEncryption:Key"]
                                ?? configuration["Jwt:Key"]
                                ?? "ReShareDefaultSuperSecretKey2026!#$";

            // Używamy SHA-256, aby uzyskać dokładnie 256-bitowy (32 bajty) klucz dla AES-256
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(configuredKey));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.GenerateIV();
                var iv = aes.IV;

                using var encryptor = aes.CreateEncryptor(aes.Key, iv);
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Zapisujemy IV (16 bajtów) na początku, a po nim zaszyfrowaną treść
                var combined = new byte[iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, combined, iv.Length, cipherBytes.Length);

                return Convert.ToBase64String(combined);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageEncryptionService] Błąd podczas szyfrowania wiadomości.");
                throw;
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                byte[] combined;
                try
                {
                    combined = Convert.FromBase64String(cipherText);
                }
                catch (FormatException)
                {
                    // Jeśli ciąg nie jest w Base64 (np. stara nieszyfrowana wiadomość), zwróć go bez zmian
                    return cipherText;
                }

                if (combined.Length <= 16)
                    return cipherText;

                using var aes = Aes.Create();
                aes.Key = _key;

                var iv = new byte[16];
                Buffer.BlockCopy(combined, 0, iv, 0, 16);

                var cipherLength = combined.Length - 16;
                var cipherBytes = new byte[cipherLength];
                Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherLength);

                using var decryptor = aes.CreateDecryptor(aes.Key, iv);
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // Jeśli deszyfrowanie się nie powiedzie, np. ciąg był jawnym tekstem
                return cipherText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageEncryptionService] Błąd podczas deszyfrowania wiadomości.");
                return cipherText;
            }
        }
    }
}
