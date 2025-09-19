using CondoSphere.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data
{
    public static class DbSeeder
    {
        // === ENTRADA ÚNICA ===
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // 0) ficheiros estáticos
            var paths = await EnsureStaticFilesAsync(env);

            // 1) roles
            await EnsureRolesAsync(roleMgr, "Administrator", "Manager", "Resident");

            // 2) dados base
            var company = await EnsureCompanyAsync(ctx);
            var condo = await EnsureCondominiumAsync(ctx, company.Id);

            // 3) utilizadores
            var users = await EnsureUsersAsync(userMgr, company.Id, paths.DefaultAvatarWebPath);

            // 4) unidade do residente
            var unit = await EnsureResidentUnitAsync(ctx, condo.Id, users.Resident.Id);

            // 5) quotas (4 em aberto: -1, 0, +1, +2)
            await EnsureQuotasAsync(ctx, unit.Id);

            // >>> NOVO: categorias do fórum
            await EnsureForumCategoriesAsync(ctx);

            // opcional: garantir pasta de uploads do fórum existe
            await EnsureForumUploadsFolderAsync(env);

            // 6) corrigir avatars vazios
            await FixMissingAvatarsAsync(ctx, paths.DefaultAvatarWebPath);
        }

        // === helpers ===

        private static async Task<(string WebRoot, string DefaultAvatarFsPath, string DefaultAvatarWebPath)>
            EnsureStaticFilesAsync(IWebHostEnvironment env)
        {
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

            return (webroot, defaultAvatarFsPath, defaultAvatarWebPath);
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleMgr, params string[] roles)
        {
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));
        }

        private static async Task<Company> EnsureCompanyAsync(ApplicationDbContext ctx)
        {
            var company = await ctx.Companies.FirstOrDefaultAsync(c => c.TaxNumber == "123456789");
            if (company != null) return company;

            company = new Company
            {
                Name = "CondoSphere Lda.",
                TaxNumber = "123456789",
                Email = "contact.condosphere@yopmail.com"
            };
            ctx.Companies.Add(company);
            await ctx.SaveChangesAsync();
            return company;
        }

        private static async Task<Condominium> EnsureCondominiumAsync(ApplicationDbContext ctx, int companyId)
        {
            var condo = await ctx.Condominiums.FirstOrDefaultAsync(c => c.Name == "Condomínio Central");
            if (condo != null) return condo;

            condo = new Condominium
            {
                Name = "Condomínio Central",
                Address = "Rua Principal, nº 100",
                CompanyId = companyId
            };
            ctx.Condominiums.Add(condo);
            await ctx.SaveChangesAsync();
            return condo;
        }

        private static async Task<(User Admin, User Manager, User Resident, User TestReset)>
            EnsureUsersAsync(UserManager<User> userMgr, int companyId, string defaultAvatarWebPath)
        {
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
                        CompanyId = companyId,
                        ProfileImagePath = defaultAvatarWebPath
                    };
                    var res = await userMgr.CreateAsync(u, password);
                    if (!res.Succeeded)
                        throw new Exception(string.Join("; ", res.Errors.Select(e => e.Description)));
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

            var admin = await EnsureUser("admin.condo@yopmail.com", "Admin User", "Admin123$", "Administrator");
            var manager = await EnsureUser("manager.condo@yopmail.com", "Manager User", "Manager123$", "Manager");
            var resident = await EnsureUser("resident.condo@yopmail.com", "Resident User", "Resident123$", "Resident");
            var test = await EnsureUser("condosphere.reset.test@yopmail.com",
                                            "ZZZ_Reset_Tester_Account_DO_NOT_USE",
                                            "Reset123$", "Resident");

            return (admin, manager, resident, test);
        }

        private static async Task<Unit> EnsureResidentUnitAsync(ApplicationDbContext ctx, int condoId, string residentId)
        {
            var unit = await ctx.Units.FirstOrDefaultAsync(u => u.Number == "A101" && u.CondominiumId == condoId);
            if (unit == null)
            {
                unit = new Unit
                {
                    Number = "A101",
                    Area = 85.0,
                    CondominiumId = condoId,
                    OwnerId = residentId
                };
                ctx.Units.Add(unit);
                await ctx.SaveChangesAsync();
                return unit;
            }

            if (unit.OwnerId != residentId)
            {
                unit.OwnerId = residentId;
                ctx.Units.Update(unit);
                await ctx.SaveChangesAsync();
            }
            return unit;
        }

        private static async Task EnsureQuotasAsync(ApplicationDbContext ctx, int unitId)
        {
            var monthStartUtc = new DateTime(
                DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var desired = new (DateTime due, decimal amount)[]
            {
                (monthStartUtc.AddMonths(-1), 22.00m),
                (monthStartUtc,               27.50m),
                (monthStartUtc.AddMonths(1),  25.00m),
                (monthStartUtc.AddMonths(2),  30.00m),
            };

            foreach (var (due, amount) in desired)
            {
                var exists = await ctx.Quotas.AnyAsync(q => q.UnitId == unitId && q.DueDate == due);
                if (!exists)
                {
                    ctx.Quotas.Add(new Quota
                    {
                        UnitId = unitId,
                        Amount = amount,
                        DueDate = due,
                        IsPaid = false
                    });
                }
            }
            await ctx.SaveChangesAsync();
        }

        private static async Task FixMissingAvatarsAsync(ApplicationDbContext ctx, string defaultAvatarWebPath)
        {
            var toFix = await ctx.Users
                .Where(x => string.IsNullOrEmpty(x.ProfileImagePath))
                .ToListAsync();

            if (toFix.Count > 0)
            {
                foreach (var u in toFix)
                    u.ProfileImagePath = defaultAvatarWebPath;

                await ctx.SaveChangesAsync();
            }
        }


        private static async Task EnsureForumCategoriesAsync(ApplicationDbContext ctx)
        {
            // Não setamos Id manualmente para evitar IDENTITY_INSERT.
            async Task AddIfMissing(string name, int sort, bool locked = false)
            {
                var exists = await ctx.ForumCategories.AnyAsync(c => c.Name == name);
                if (!exists)
                {
                    ctx.ForumCategories.Add(new ForumCategory
                    {
                        Name = name,
                        SortOrder = sort,
                        IsLocked = locked,
                        // CompanyId / CondominiumId ficam nulos (escopo global),
                        // ajuste aqui se quiser escopo por empresa/condomínio.
                    });
                }
            }

            await AddIfMissing("General", 1);
            await AddIfMissing("Maintenance", 2);
            await AddIfMissing("Buy & Sell", 3);

            await ctx.SaveChangesAsync();
        }

        private static Task EnsureForumUploadsFolderAsync(IWebHostEnvironment env)
        {
            var root = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var forumDir = Path.Combine(root, "uploads", "forum");
            Directory.CreateDirectory(forumDir);

            var keep = Path.Combine(forumDir, ".gitkeep");
            if (!File.Exists(keep)) File.WriteAllText(keep, "");
            return Task.CompletedTask;
        }
    }
}

