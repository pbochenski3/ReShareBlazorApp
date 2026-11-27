using System.Security.Cryptography;
using System.Text;

namespace ReShareBlazorApp.Api.Services;

public sealed class ChatEncryptionService
{
    private readonly byte[] _key;

    public ChatEncryptionService(IConfiguration configuration)
    {
        var encoded = configuration["Chat:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(encoded))
            throw new InvalidOperationException("Brak konfiguracji Chat:EncryptionKey (32 bajty w Base64).");
        try { _key = Convert.FromBase64String(encoded); }
        catch (FormatException ex) { throw new InvalidOperationException("Chat:EncryptionKey musi być poprawnym Base64.", ex); }
        if (_key.Length != 32)
            throw new InvalidOperationException("Chat:EncryptionKey musi mieć dokładnie 32 bajty (AES-256).");
    }

    public (string CipherText, string Nonce, string Tag) Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return (Convert.ToBase64String(cipher), Convert.ToBase64String(nonce), Convert.ToBase64String(tag));
    }

    public string Decrypt(string cipherText, string nonce, string tag)
    {
        var cipher = Convert.FromBase64String(cipherText);
        var nonceBytes = Convert.FromBase64String(nonce);
        var tagBytes = Convert.FromBase64String(tag);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonceBytes, cipher, tagBytes, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
