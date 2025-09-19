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

        public DbSet<SystemSettings> SystemSettings { get; set; } = default!;

        public DbSet<UnitOwnership> UnitOwnerships { get; set; } = default!;


        public DbSet<ForumCategory> ForumCategories { get; set; }
        public DbSet<ForumTopic> ForumTopics { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumSubscription> ForumSubscriptions { get; set; }

        public DbSet<ForumAttachment> ForumAttachments { get; set; }

        public DbSet<ForumReaction> ForumReactions { get; set; } = default!;





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


            modelBuilder.Entity<SystemSettings>(e =>
            {
                e.HasIndex(x => x.TenantId);
                e.Property(x => x.DefaultLateFeePercent).HasColumnType("decimal(5,2)");
                e.Property(x => x.DefaultInterestMonthlyPercent).HasColumnType("decimal(5,2)");
            });

            // seed de linha global (id=1)
            modelBuilder.Entity<SystemSettings>().HasData(new SystemSettings
            {
                Id = 1,
                TenantId = null,
                CompanyDisplayName = "CondoSphere",
                SupportEmail = "support@condosphere.app",
                DefaultLateFeePercent = 2.00m,
                DefaultInterestMonthlyPercent = 1.00m,
                GraceDaysForQuotas = 5,
                WelcomeUserEmailSubject = "Bem-vindo(a) ao CondoSphere",
                WelcomeUserEmailHtml =
                    "<p>Olá {{User.FullName}},</p><p>A sua conta foi criada.</p><p>Senha provisória: <code>{{TempPassword}}</code></p><p>Por favor altere aqui: <a href='{{ResetUrl}}'>Alterar palavra-passe</a></p><p>Cumprimentos,<br/>{{Company.Name}}</p>",
                PasswordResetEmailSubject = "CondoSphere – Redefinição de palavra-passe",
                PasswordResetEmailHtml =
                    "<p>Olá {{User.Email}},</p><p>Clique para redefinir (válido por 4 dias): <a href='{{ResetUrl}}'>Reset</a></p><p>Se não foi você, ignore.</p>",
                    PaymentReceiptEmailEnabled = true,
                PaymentReceiptEmailSubject = "Comprovativo de pagamento",
                PaymentReceiptEmailHtml =
    "<p>Olá {{User.FullName}},</p>" +
    "<p>Recebemos o seu pagamento de <strong>{{Payment.Amount}}</strong> em {{Payment.Date}}.</p>" +
    "<p>Referência: <code>{{Payment.Reference}}</code> · Método: {{Payment.Method}}</p>" +
    "<p>Pode consultar/guardar a fatura aqui: <a href='{{InvoiceUrl}}'>Ver fatura</a></p>" +
    "<p>Cumprimentos,<br/>{{Company.Name}}</p>"
            });


           modelBuilder.Entity<Quota>()
 .HasIndex(q => new { q.DebtorUserId, q.DueDate });



            // UNIQUE: número por condomínio
            // Unit: número único dentro do condomínio
            modelBuilder.Entity<Unit>()
             .HasIndex(u => new { u.CondominiumId, u.Number })
             .IsUnique();

            // UnitOwnership: relações
            modelBuilder.Entity<UnitOwnership>()
             .HasOne(x => x.Unit)
             .WithMany(u => u.OwnershipHistory)
             .HasForeignKey(x => x.UnitId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnitOwnership>()
             .HasOne(x => x.Owner)
             .WithMany()               // sem coleção reversa obrigatória
             .HasForeignKey(x => x.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);

            // Proteções usuais
            modelBuilder.Entity<Quota>()
             .HasOne(q => q.Unit)
             .WithMany(u => u.Quotas)
             .HasForeignKey(q => q.UnitId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
             .HasOne(p => p.Quota)
             .WithOne(q => q.Payment)
             .HasForeignKey<Payment>(p => p.QuotaId)
             .OnDelete(DeleteBehavior.Restrict);


            // Forum
            modelBuilder.Entity<ForumCategory>(e =>
            {
                e.HasIndex(x => x.Slug);
                e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            });

            modelBuilder.Entity<ForumTopic>(e =>
            {
                e.HasOne(t => t.Category)
                 .WithMany(c => c.Topics)
                 .HasForeignKey(t => t.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(t => new { t.CategoryId, t.IsPinned, t.LastPostAtUtc });
                e.Property(t => t.Title).HasMaxLength(140).IsRequired();
            });

            modelBuilder.Entity<ForumPost>(e =>
            {
                e.HasOne(p => p.Topic)
                 .WithMany(t => t.Posts)
                 .HasForeignKey(p => p.TopicId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(p => new { p.TopicId, p.CreatedAtUtc });
                e.Property(p => p.Body).HasMaxLength(8000).IsRequired();
            });

            modelBuilder.Entity<ForumSubscription>(e =>
            {
                e.HasIndex(s => new { s.TopicId, s.UserId }).IsUnique();
            });



            modelBuilder.Entity<ForumAttachment>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Post)
                 .WithMany(p => p.Attachments)
                 .HasForeignKey(x => x.PostId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.Property(x => x.FileName).HasMaxLength(255);
                b.Property(x => x.Path).HasMaxLength(512);
                b.Property(x => x.ContentType).HasMaxLength(200);
            });


            modelBuilder.Entity<ForumReaction>()
    .HasIndex(r => new { r.PostId, r.UserId, r.Emoji })
    .IsUnique();




        }

    }


}
