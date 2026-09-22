using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Lab789AuthServer.Data
{
    public static class SeedData
    {
        public static async Task InitAsync(IServiceProvider service)
        {
            var roleManager = service.GetRequiredService<RoleManager< IdentityRole>>();
            var userManager = service.GetRequiredService<UserManager< IdentityUser>>();

            await SeedRolesAsync(roleManager);
            await SeedUserAsync(userManager, "admin@gmail.com", "admin123", "Admin");
            await SeedUserAsync(userManager, "employee@gmail.com", "nhanvien123", "Employee");
            await SeedUserAsync(userManager, "sale@gmail.com", "khach123", "Sales");
        }
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Employee", "Sales" };
            foreach (var role in roles)
            {
                if(!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if(!result.Succeeded)
                    {
                        throw new InvalidOperationException($"Unable to create role: {role}");
                    }
                }
            }
        }

        private static async Task SeedUserAsync(UserManager<IdentityUser> userManager, string email, string password, string role)
        {
            var user = await userManager.FindByNameAsync(email);
            if(user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded) 
                {
                    var errors = string.Join(";", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(errors);
                }
            }

            if(!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if(!roleResult.Succeeded)
                {
                    var errors = string.Join(";", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(errors);
                }
            }
        }
    }
}
