using Microsoft.AspNetCore.Identity;

namespace FitnessProje.Models
{
    // IdentityUser sınıfından miras alıyoruz (Id, Email, PasswordHash vb. oradan geliyor)
    public class UygulamaKullanıcı : IdentityUser
    {
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        // Bir üyenin birden fazla randevusu olabilir
        public ICollection<RandevuSistemi> Randevular { get; set; }
    }
}