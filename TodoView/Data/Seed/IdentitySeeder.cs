using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TodoView.Authorization;
using TodoView.Models;

namespace TodoView.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var options = services.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        foreach (var role in new[] { AppRoles.Admin, AppRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!createRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{role}': {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        var adminUser = await userManager.FindByEmailAsync(options.Email);
        if (adminUser is null)
        {
            adminUser = new User
            {
                UserName = options.Email,
                Email = options.Email,
                FirstName = options.FirstName,
                LastName = options.LastName,
                Address = options.Address,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var createAdminResult = await userManager.CreateAsync(adminUser, options.Password);
            if (!createAdminResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create seeded admin: {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            var addAdminRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            if (!addAdminRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to assign admin role: {string.Join(", ", addAdminRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.User))
        {
            var addUserRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.User);
            if (!addUserRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to assign user role to seeded admin: {string.Join(", ", addUserRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        foreach (var user in userManager.Users.ToList())
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                var addDefaultRoleResult = await userManager.AddToRoleAsync(user, AppRoles.User);
                if (!addDefaultRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to assign default user role for '{user.Email}': {string.Join(", ", addDefaultRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
