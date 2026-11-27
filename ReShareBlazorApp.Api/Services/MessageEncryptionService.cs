using ReShareBlazorApp.Api.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace ReShareBlazorApp.Api.Services
{
    // AES-256-GCM. Format: Base64( wersja(1) | nonce(12) | tag(16) | szyfrogram ).
    public class MessageEncryptionService : IMessageEncryptionService
    {
        private const byte FormatVersion = 1;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _key;

        public MessageEncryptionService(string base64Key)
        {
            if (string.IsNullOrWhiteSpace(base64Key))
                throw new InvalidOperationException("Chat:EncryptionKey is not configured.");

            try
            {
                _key = Convert.FromBase64String(base64Key);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Chat:EncryptionKey musi być poprawnym ciągiem Base64.");
            }

            if (_key.Length != 32)
                throw new InvalidOperationException("Chat:EncryptionKey musi mieć dokładnie 32 bajty (256 bitów).");
        }

        public string Encrypt(string plainText, string purpose)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipher, tag, Encoding.UTF8.GetBytes(purpose));

            var result = new byte[1 + NonceSize + TagSize + cipher.Length];
            result[0] = FormatVersion;
            Buffer.BlockCopy(nonce, 0, result, 1, NonceSize);
            Buffer.BlockCopy(tag, 0, result, 1 + NonceSize, TagSize);
            Buffer.BlockCopy(cipher, 0, result, 1 + NonceSize + TagSize, cipher.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string payload, string purpose)
        {
            var data = Convert.FromBase64String(payload);

            if (data.Length < 1 + NonceSize + TagSize || data[0] != FormatVersion)
                throw new CryptographicException("Nieprawidłowy format zaszyfrowanych danych.");

            var nonce = data.AsSpan(1, NonceSize);
            var tag = data.AsSpan(1 + NonceSize, TagSize);
            var cipher = data.AsSpan(1 + NonceSize + TagSize);
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain, Encoding.UTF8.GetBytes(purpose));

            return Encoding.UTF8.GetString(plain);
        }
    }
}