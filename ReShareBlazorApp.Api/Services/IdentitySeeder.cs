using Microsoft.AspNetCore.Identity;
using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Services;

public static class IdentitySeeder
{
    private const string AdminRole = "Admin";
    private const string AdminEmail = "admin12345@admin.pl";
    private const string AdminPassword = "Admin12345!";
    private const string AdminUserName = "admin12345";

    public static async Task SeedAsync(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(AdminRole));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Nie udało się utworzyć roli {AdminRole}: " +
                    string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        var user = await userManager.FindByEmailAsync(AdminEmail);
        if (user == null)
        {
            user = new User
            {
                UserName = AdminUserName,
                Email = AdminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var userResult = await userManager.CreateAsync(user, AdminPassword);
            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Nie udało się utworzyć użytkownika administratora: " +
                    string.Join(", ", userResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, AdminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AdminRole);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Nie udało się nadać roli {AdminRole}: " +
                    string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }
}
