using FitnessProje.Models;
using Microsoft.AspNetCore.Identity;

namespace FitnessProje.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Kullanıcı Yöneticisi ve Rol Yöneticisini çağırıyoruz
            var userManager = service.GetService<UserManager<UygulamaKullanıcı>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();

            // 1. ROLLERİ EKLE (Admin ve Uye)
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("Uye"));

            // 2. ADMIN KULLANICISINI EKLE
            var adminUser = new UygulamaKullanıcı
            {
                UserName = "g231210078@sakarya.edu.tr", // Kendi numaranı yazabilirsin
                Email = "ogrencinumarasi@sakarya.edu.tr",
                Ad = "Yönetici",
                Soyad = "Admin",
                EmailConfirmed = true,
                KayitTarihi = DateTime.Now
            };

            // E-posta veritabanında var mı kontrol et
            var userInDb = await userManager.FindByEmailAsync(adminUser.Email);
            if (userInDb == null)
            {
                // Yoksa oluştur. Şifre: sau
                await userManager.CreateAsync(adminUser, "sau");
                // Kullanıcıya Admin rolünü ata
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}