using CondoSphere.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // 1) Ensure schema (creates AspNetRoles, AspNetUsers, etc.)
            await ctx.Database.MigrateAsync();

            // 2) Roles
            string[] roles = { "Administrator", "Manager", "Resident" };
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            // 3) Company & Condominium (idempotent)
            var company = await ctx.Companies.FirstOrDefaultAsync(c => c.TaxNumber == "123456789");
            if (company == null)
            {
                company = new Company { Name = "CondoSphere Lda.", TaxNumber = "123456789", Email = "contact@condosphere.com" };
                ctx.Companies.Add(company);
                await ctx.SaveChangesAsync();
            }

            var condo = await ctx.Condominiums.FirstOrDefaultAsync(c => c.Name == "Condomínio Central");
            if (condo == null)
            {
                condo = new Condominium { Name = "Condomínio Central", Address = "Rua Principal, nº 100", CompanyId = company.Id };
                ctx.Condominiums.Add(condo);
                await ctx.SaveChangesAsync();
            }

            // 4) Users (Identity, hashed passwords)
            async Task<User> EnsureUser(string email, string name, string password, string role)
            {
                var u = await userMgr.FindByEmailAsync(email);
                if (u == null)
                {
                    u = new User
                    {
                        UserName = email,
                        Email = email,
                        FullName = name,
                        EmailConfirmed = true,
                        IsActive = true,
                        CompanyId = company.Id
                    };
                    var res = await userMgr.CreateAsync(u, password);
                    if (!res.Succeeded) throw new Exception(string.Join("; ", res.Errors.Select(e => e.Description)));
                }
                if (!await userMgr.IsInRoleAsync(u, role))
                    await userMgr.AddToRoleAsync(u, role);
                return u;
            }

            var admin = await EnsureUser("admin@condo.com", "Admin User", "Admin123$", "Administrator");
            var manager = await EnsureUser("manager@condo.com", "Manager User", "Manager123$", "Manager");
            var resident = await EnsureUser("resident@condo.com", "Resident User", "Resident123$", "Resident");

            // 5) Sample unit for resident
            if (!await ctx.Units.AnyAsync())
            {
                ctx.Units.Add(new Unit
                {
                    Number = "A101",
                    Area = 85.0,
                    CondominiumId = condo.Id,
                    OwnerId = resident.Id // string (IdentityUser key)
                });
                await ctx.SaveChangesAsync();
            }
        }
    }
}
