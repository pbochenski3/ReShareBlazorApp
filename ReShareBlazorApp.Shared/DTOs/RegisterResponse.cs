namespace ReShareBlazorApp.Shared.DTOs
{
    public class RegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool RequiresEmailConfirmation { get; set; }
        public string? Email { get; set; }
    }
}
