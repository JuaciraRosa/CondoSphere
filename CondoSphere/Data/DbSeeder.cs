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
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // === (0) arquivos estáticos necessários ===
            var webroot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagesDir = Path.Combine(webroot, "images");
            Directory.CreateDirectory(imagesDir);
            var defaultAvatarFsPath = Path.Combine(imagesDir, "default-user.png");
            var defaultAvatarWebPath = "/images/default-user.png";

            if (!File.Exists(defaultAvatarFsPath))
            {
                // PNG 1x1 transparente
                var base64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X4iQAAAAASUVORK5CYII=";
                await File.WriteAllBytesAsync(defaultAvatarFsPath, Convert.FromBase64String(base64));
            }

            // === (1) migra o schema ===
            await ctx.Database.MigrateAsync();

            // Tudo a seguir numa transação para evitar “meio semeado”
            await using var tx = await ctx.Database.BeginTransactionAsync();

            // === (2) roles ===
            async Task EnsureRole(string role)
            {
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
            }
            foreach (var r in new[] { "Administrator", "Manager", "Resident" })
                await EnsureRole(r);

            // === (3) dados base: company & condo ===
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

            // === (4) utilizadores ===
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
                        CompanyId = company.Id,
                        ProfileImagePath = defaultAvatarWebPath
                    };
                    var res = await userMgr.CreateAsync(u, password);
                    if (!res.Succeeded) throw new Exception(string.Join("; ", res.Errors.Select(e => e.Description)));
                }
                else if (string.IsNullOrWhiteSpace(u.ProfileImagePath))
                {
                    u.ProfileImagePath = defaultAvatarWebPath;
                    await userMgr.UpdateAsync(u);
                }

                if (!await userMgr.IsInRoleAsync(u, role))
                    await userMgr.AddToRoleAsync(u, role);

                return u;
            }

            var admin = await EnsureUser("admin@condo.com", "Admin User", "Admin123$", "Administrator");
            var manager = await EnsureUser("manager@condo.com", "Manager User", "Manager123$", "Manager");
            var resident = await EnsureUser("resident@condo.com", "Resident User", "Resident123$", "Resident");
            var testReset = await EnsureUser("condosphere.reset.test@yopmail.com",
                                             "ZZZ_Reset_Tester_Account_DO_NOT_USE",
                                             "Reset123$", "Resident");

            // === (5) unidade do residente ===
            var unit = await ctx.Units.FirstOrDefaultAsync(u => u.Number == "A101" && u.CondominiumId == condo.Id);
            if (unit == null)
            {
                unit = new Unit
                {
                    Number = "A101",
                    Area = 85.0,
                    CondominiumId = condo.Id,
                    OwnerId = resident.Id  // Identity FK
                };
                ctx.Units.Add(unit);
                await ctx.SaveChangesAsync(); // gera unit.Id
            }
            else
            {
                // garante que a unidade tem dono para testes de “minhas quotas/unidades”
                if (unit.OwnerId != resident.Id)
                {
                    unit.OwnerId = resident.Id;
                    ctx.Units.Update(unit);
                    await ctx.SaveChangesAsync();
                }
            }

            // === (6) quotas de exemplo (idempotente) ===
            var monthStartUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var quotasDesejadas = new (DateTime due, decimal amount)[]
            {
                (monthStartUtc.AddMonths(-1), 22.00m), // mês anterior (em aberto)
                (monthStartUtc,               27.50m), // mês atual (em aberto)
                (monthStartUtc.AddMonths(1),  25.00m), // +1
                (monthStartUtc.AddMonths(2),  30.00m), // +2
            };

            foreach (var (due, amount) in quotasDesejadas)
            {
                var exists = await ctx.Quotas.AnyAsync(q => q.UnitId == unit.Id && q.DueDate == due);
                if (!exists)
                {
                    ctx.Quotas.Add(new Quota
                    {
                        UnitId = unit.Id,
                        Amount = amount,
                        DueDate = due,
                        IsPaid = false
                    });
                }
            }
            await ctx.SaveChangesAsync();

            // === (7) corrige imagens vazias em usuários legados ===
            var toFix = await ctx.Users
                .Where(x => string.IsNullOrEmpty(x.ProfileImagePath))
                .ToListAsync();
            if (toFix.Count > 0)
            {
                foreach (var u in toFix) u.ProfileImagePath = defaultAvatarWebPath;
                await ctx.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
    }
}

