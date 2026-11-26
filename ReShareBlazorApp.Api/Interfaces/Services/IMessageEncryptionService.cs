namespace ReShareBlazorApp.Api.Interfaces.Services;

public interface IMessageEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string cipherPayload);
    string ComputeSha256(string plaintext);
}
