namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public static class EncryptionPurposes
    {
        public const string Message = "chat-message";
        public const string Log = "chat-log";
        public const string AdminReply = "admin-reply";
    }

    public interface IMessageEncryptionService
    {
        string Encrypt(string plainText, string purpose);
        string Decrypt(string payload, string purpose);
    }
}