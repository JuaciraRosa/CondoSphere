using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CondoSphere.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // tenta achar a pasta do projeto Web, onde estão os appsettings
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "..", "CondoSphere"),
                Directory.GetCurrentDirectory()
            };
            var basePath = candidates.First(p => File.Exists(Path.Combine(p, "appsettings.json")));

            var env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var cfg = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var conn = cfg.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não encontrada nos appsettings.");

            var opt = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(conn)
                .Options;

            return new ApplicationDbContext(opt);
        }
    }
}
