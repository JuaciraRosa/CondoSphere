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

        public DbSet<ChatThread> ChatThreads { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<ChatAttachment> ChatAttachments { get; set; }

        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<AnnouncementRead> AnnouncementReads { get; set; }

        public DbSet<StaffChatAlert> StaffChatAlerts { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Relations =====

            // Unit -> Owner (User)
            modelBuilder.Entity<Unit>()
                .HasOne(u => u.Owner)
                .WithMany(x => x.OwnedUnits)
                .HasForeignKey(u => u.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unit -> Condominium
            modelBuilder.Entity<Unit>()
                .HasOne(u => u.Condominium)
                .WithMany(c => c.Units)
                .HasForeignKey(u => u.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Condominium -> Company
            modelBuilder.Entity<Condominium>()
                .HasOne(c => c.Company)
                .WithMany(co => co.Condominiums)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // MaintenanceRequest -> SubmittedBy (User)
            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.SubmittedBy)
                .WithMany()
                .HasForeignKey(m => m.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);

            // MaintenanceRequest -> Condominium
            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Condominium)
                .WithMany(c => c.MaintenanceRequests)
                .HasForeignKey(m => m.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Meeting -> Condominium
            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.Condominium)
                .WithMany(c => c.Meetings)
                .HasForeignKey(m => m.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification -> Condominium
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Condominium)
                .WithMany(c => c.Notifications)
                .HasForeignKey(n => n.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification <-> User (many-to-many)
            modelBuilder.Entity<Notification>()
                .HasMany(n => n.Recipients)
                .WithMany()
                .UsingEntity(j => j.ToTable("NotificationRecipients"));

            // Quota -> Unit
            modelBuilder.Entity<Quota>()
                .HasOne(q => q.Unit)
                .WithMany(u => u.Quotas)
                .HasForeignKey(q => q.UnitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment -> Quota (1:1)
            modelBuilder.Entity<Quota>()
                .HasOne(q => q.Payment)
                .WithOne(p => p.Quota)
                .HasForeignKey<Payment>(p => p.QuotaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
            .HasOne(u => u.Company)
           .WithMany(c => c.Users)                // ou .WithMany() se não tiver navegação
           .HasForeignKey(u => u.CompanyId)
          .OnDelete(DeleteBehavior.Restrict);

            // ===== Precision for money =====
            modelBuilder.Entity<Quota>()
                .Property(q => q.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // ===== Defaults =====
            modelBuilder.Entity<User>()
                .Property(u => u.ProfileImagePath)
                .IsRequired()
                .HasDefaultValue("");



            modelBuilder.Entity<ChatMessage>()
           .HasOne(m => m.Thread)
           .WithMany(t => t.Messages)
           .HasForeignKey(m => m.ThreadId)
           .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatThread>()
                .HasIndex(t => t.ResidentId);


            modelBuilder.Entity<ChatAttachment>()
           .HasOne(a => a.Message)
          .WithMany(m => m.Attachments)
          .HasForeignKey(a => a.MessageId)
          .OnDelete(DeleteBehavior.Cascade);

            // ---- POLLS ----
            modelBuilder.Entity<Poll>(e =>
            {
                e.Property(p => p.Title).HasMaxLength(140).IsRequired();
                e.Property(p => p.Description).HasMaxLength(1000);

                // defaults no DB (UTC)
                e.Property(p => p.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
                e.Property(p => p.StartsAtUtc).HasDefaultValueSql("GETUTCDATE()");

                // Condomínio
                e.HasOne(p => p.Condominium)
                 .WithMany(c => c.Polls)
                 .HasForeignKey(p => p.CondominiumId)
                 .OnDelete(DeleteBehavior.Cascade);

                // ✅ Criador
                e.HasOne(p => p.CreatedBy)
                 .WithMany() // (ou .WithMany(u => u.PollsCriados) se quiser navegação reversa)
                 .HasForeignKey(p => p.CreatedById)
                 .OnDelete(DeleteBehavior.Restrict);

                // índices úteis
                e.HasIndex(p => new { p.CondominiumId, p.StartsAtUtc, p.EndsAtUtc });
            });

            modelBuilder.Entity<PollOption>(e =>
            {
                e.Property(o => o.Text).HasMaxLength(160).IsRequired();

                e.HasOne(o => o.Poll)
                 .WithMany(p => p.Options)
                 .HasForeignKey(o => o.PollId)
                 .OnDelete(DeleteBehavior.Cascade);

                // opcional (evita opções duplicadas dentro do mesmo poll)
                // e.HasIndex(o => new { o.PollId, o.Text }).IsUnique();
            });



            modelBuilder.Entity<PollVote>(e =>
            {
                // marca o timestamp de voto do lado do DB
                e.Property(v => v.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // 1 voto por utilizador em cada enquete
                e.HasIndex(v => new { v.PollId, v.UserId }).IsUnique();

                // FK Poll
                e.HasOne<Poll>()
                 .WithMany(p => p.Votes)
                 .HasForeignKey(v => v.PollId)
                 .OnDelete(DeleteBehavior.Cascade);

              
                // FK Option — RESTRITO para evitar múltiplos caminhos de cascade
                e.HasOne<PollOption>()
                 .WithMany(o => o.Votes)
                 .HasForeignKey(v => v.OptionId)
                 .OnDelete(DeleteBehavior.Restrict);

            });


            modelBuilder.Entity<AnnouncementRead>(e =>
            {
                e.HasIndex(x => new { x.AnnouncementId, x.UserId }).IsUnique();
                e.HasOne(x => x.Announcement)
                 .WithMany()
                 .HasForeignKey(x => x.AnnouncementId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });



        }


    }

}
