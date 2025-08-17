using CondoSphere.Models;

namespace CondoSphere.Data
{
    public static class DbSeeder
    {
        public static async Task Seed(IServiceProvider services)
        {

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Evita duplicações
            if (context.Users.Any()) return;


            var company = new Company
            {
                Name = "CondoSphere Lda.",
                TaxNumber = "123456789",
                Email = "contact@condosphere.com"
            };
            context.Companies.Add(company);

            var condominium = new Condominium
            {
                Name = "Condomínio Central",
                Address = "Rua Principal, nº 100",
                Company = company
            };
            context.Condominiums.Add(condominium);

            var admin = new User
            {
                FullName = "Admin User",
                Email = "admin@condo.com",
                PasswordHash = "admin123",
                Role = UserRole.Administrator,
                IsActive = true,
                Company = company
            };

            var manager = new User
            {
                FullName = "Manager User",
                Email = "manager@condo.com",
                PasswordHash = "manager123",
                Role = UserRole.Manager,
                IsActive = true,
                Company = company
            };

            var resident = new User
            {
                FullName = "Resident User",
                Email = "resident@condo.com",
                PasswordHash = "resident123",
                Role = UserRole.Resident,
                IsActive = true,
                Company = company
            };

            context.Users.AddRange(admin, manager, resident);


            var unit = new Unit
            {
                Number = "A101",
                Area = 85.0,
                Condominium = condominium,
                Owner = resident
            };
            context.Units.Add(unit);


            context.SaveChanges();
        }
    }
}
