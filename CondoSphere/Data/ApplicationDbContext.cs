using CondoSphere.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CondoSphere.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Condominium> Condominiums { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Quota> Quotas { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Meeting> Meetings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unit -> Owner (User)
            modelBuilder.Entity<Unit>()
                .HasOne(u => u.Owner)
                .WithMany(x => x.OwnedUnits)
                .HasForeignKey(u => u.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // MaintenanceRequest -> SubmittedBy (User)
            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.SubmittedBy)
                .WithMany()
                .HasForeignKey(m => m.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Precisão para valores monetários
            modelBuilder.Entity<Quota>()
                .Property(q => q.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            // (Adicione outras precisões se necessário)
            modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        }


    }

}
