using System.Security.Cryptography;
using System.Text;
using ReShareBlazorApp.Api.Interfaces.Services;

namespace ReShareBlazorApp.Api.Services;

/// <summary>
/// Szyfruje i deszyfruje wiadomości oraz logi wiadomości za pomocą AES-256-GCM.
/// </summary>
public class MessageEncryptionService : IMessageEncryptionService
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public MessageEncryptionService(IConfiguration configuration)
    {
        var rawKey = configuration["ChatEncryption:Key"];
        if (string.IsNullOrWhiteSpace(rawKey))
            rawKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(rawKey))
            throw new InvalidOperationException("Brak klucza szyfrowania wiadomości (ChatEncryption:Key lub Jwt:Key).");

        // Wyprowadzamy deterministyczny 256-bitowy klucz z separacją domeny.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes($"ReShareChatAesGcm:{rawKey}"));
    }

    public string Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext ?? string.Empty);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var combined = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, combined, NonceSize + TagSize, cipherBytes.Length);

        return Prefix + Convert.ToBase64String(combined);
    }

    public string Decrypt(string cipherPayload)
    {
        if (string.IsNullOrWhiteSpace(cipherPayload))
            return string.Empty;

        if (!cipherPayload.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Nieznany format zaszyfrowanej wiadomości.");

        var combined = Convert.FromBase64String(cipherPayload[Prefix.Length..]);
        if (combined.Length < NonceSize + TagSize)
            throw new CryptographicException("Uszkodzony ładunek zaszyfrowanej wiadomości.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[combined.Length - NonceSize - TagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(combined, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string ComputeSha256(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
