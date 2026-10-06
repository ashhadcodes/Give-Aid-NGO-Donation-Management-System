using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Give_Aid_NGO_Donation_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DonationCause> DonationCauses => Set<DonationCause>();
        public DbSet<Donation> Donations => Set<Donation>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<DonationReceipt> DonationReceipts => Set<DonationReceipt>();
        public DbSet<NGO> NGOs => Set<NGO>();
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<Programme> Programmes => Set<Programme>();
        public DbSet<ProgrammeInterest> ProgrammeInterests => Set<ProgrammeInterest>();
        public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
        public DbSet<AboutPage> AboutPages => Set<AboutPage>();
        public DbSet<FaqCategory> FaqCategories => Set<FaqCategory>();
        public DbSet<Faq> Faqs => Set<Faq>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<SupportQuery> SupportQueries => Set<SupportQuery>();
        public DbSet<Invitation> Invitations => Set<Invitation>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Indexes for fast lookup
            builder.Entity<DonationCause>()
                .HasIndex(c => c.Code)
                .IsUnique();

            builder.Entity<DonationCause>()
                .Property(c => c.TargetAmount)
                .HasPrecision(18, 2);

            builder.Entity<DonationCause>()
                .Property(c => c.RaisedAmount)
                .HasPrecision(18, 2);

            builder.Entity<Donation>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);

            builder.Entity<PaymentTransaction>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            builder.Entity<DonationReceipt>()
                .Property(r => r.Amount)
                .HasPrecision(18, 2);

            builder.Entity<PaymentTransaction>()
                .HasIndex(p => p.TransactionId)
                .IsUnique();

            builder.Entity<DonationReceipt>()
                .HasIndex(r => r.ReceiptNumber)
                .IsUnique();

            builder.Entity<AboutPage>()
                .HasIndex(a => a.SectionKey)
                .IsUnique();

            builder.Entity<SiteSetting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            // Relationship rules
            builder.Entity<Donation>()
                .HasOne(d => d.User)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Donation>()
                .HasOne(d => d.Cause)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CauseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.Donation)
                .WithMany(d => d.PaymentTransactions)
                .HasForeignKey(p => p.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DonationReceipt>()
                .HasOne(r => r.Donation)
                .WithOne(d => d.Receipt)
                .HasForeignKey<DonationReceipt>(r => r.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Programme>()
                .HasOne(p => p.NGO)
                .WithMany(n => n.Programmes)
                .HasForeignKey(p => p.NGOId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ProgrammeInterest>()
                .HasOne(pi => pi.Programme)
                .WithMany(p => p.Interests)
                .HasForeignKey(pi => pi.ProgrammeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProgrammeInterest>()
                .HasOne(pi => pi.User)
                .WithMany(u => u.ProgrammeInterests)
                .HasForeignKey(pi => pi.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<GalleryItem>()
                .HasOne(g => g.Programme)
                .WithMany(p => p.GalleryItems)
                .HasForeignKey(g => g.ProgrammeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Faq>()
                .HasOne(f => f.Category)
                .WithMany(c => c.Faqs)
                .HasForeignKey(f => f.FaqCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SupportQuery>()
                .HasOne(q => q.User)
                .WithMany(u => u.SupportQueries)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Invitation>()
                .HasOne(i => i.SenderUser)
                .WithMany(u => u.InvitationsSent)
                .HasForeignKey(i => i.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
