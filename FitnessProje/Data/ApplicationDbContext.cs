using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FitnessProje.Models;

namespace FitnessProje.Data
{
    public class ApplicationDbContext : IdentityDbContext<UygulamaKullanıcı>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
       
        public DbSet<Bildirim> Bildirimler { get; set; }
        public DbSet<Hizmetler> Hizmetler { get; set; }
        public DbSet<Antrenör> Antrenörler { get; set; }
        public DbSet<RandevuSistemi> Randevular { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- 1. Decimal (Para) Alanı Ayarı (Sarı uyarıyı çözer) ---
            builder.Entity<Hizmetler>()
                .Property(h => h.Ucret)
                .HasColumnType("decimal(18,2)"); // 18 basamak, virgülden sonra 2 hane

            // --- 2. İlişki Hatasını Çözen Ayarlar (Kırmızı hatayı çözer) ---

            // Bir Antrenör silinirse, Randevuları silinmesin (Hata vermesin, kısıtlasın)
            builder.Entity<RandevuSistemi>()
                .HasOne(r => r.Antrenör)
                .WithMany(a => a.Randevular)
                .HasForeignKey(r => r.AntrenörId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir Hizmet silinirse, Randevuları silinmesin
            builder.Entity<RandevuSistemi>()
                .HasOne(r => r.Hizmet)
                .WithMany()
                .HasForeignKey(r => r.HizmetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}