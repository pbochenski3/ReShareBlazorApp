namespace ReShareBlazorApp.Shared.DTOs;

public class AdminUserDTO
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
}
